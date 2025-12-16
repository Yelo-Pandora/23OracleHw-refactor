// 外观模式：设备系统具体实现类
// EquipmentSystemFacade.cs

using Microsoft.Extensions.Logging;
using oracle_backend.Models;
using oracle_backend.patterns.Facade_Pattern.Interfaces;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.State.Equipment;
using static oracle_backend.Controllers.EquipmentController;

namespace oracle_backend.patterns.Facade_Pattern.Implementations
{
    public class EquipmentSystemFacade : IEquipmentSystemFacade
    {
        private readonly IEquipmentRepository _equipRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ILogger<EquipmentSystemFacade> _logger;

        // 静态操作定义 (从Controller复制)
        private static readonly string[] AirConditionerActions = { "关机", "制冷模式", "制热模式", "调节温度", "紧急停止" };
        private static readonly string[] LightingActions = { "关灯", "调亮", "调暗", "紧急停止" };
        private static readonly string[] ElevatorActions = { "停止", "开门", "关门", "紧急停止" };
        private static readonly string[] StandbyAirActions = { "开机", "紧急停止" };
        private static readonly string[] StandbyLightActions = { "开灯", "紧急停止" };
        private static readonly string[] StandbyElevatorActions = { "启动", "紧急停止" };

        public EquipmentSystemFacade(
            IEquipmentRepository equipRepo,
            IAccountRepository accountRepo,
            ILogger<EquipmentSystemFacade> logger)
        {
            _equipRepo = equipRepo;
            _accountRepo = accountRepo;
            _logger = logger;
        }

        private EquipmentStateContext CreateEquipmentStateContext(Equipment equipment)
        {
            return new EquipmentStateContext(
                equipment.EQUIPMENT_ID,
                equipment.EQUIPMENT_TYPE,
                equipment.EQUIPMENT_STATUS,
                _logger
            );
        }

        // 封装Controller中重复的权限检查逻辑
        private async Task CheckPermissionAsync(string operatorId, int requiredAuthority)
        {
            var isAuthority = await _accountRepo.CheckAuthority(operatorId, requiredAuthority);
            if (!isAuthority)
            {
                throw new UnauthorizedAccessException("操作者权限不足");
            }

            var operatorAccount = await _accountRepo.FindAccountByUsername(operatorId);
            if (operatorAccount != null && operatorAccount.AUTHORITY == requiredAuthority)
            {
                var staffAccount = await _accountRepo.CheckStaff(operatorId);
                if (staffAccount == null)
                    throw new UnauthorizedAccessException("该操作员无对应员工");

                var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);
                if (staff == null)
                    throw new UnauthorizedAccessException("不存在该员工");

                if (staff.STAFF_APARTMENT != "维修部")
                    throw new UnauthorizedAccessException("该员工非维修部员工无权操作设备");
            }
        }

        private bool SimulateDeviceOperation()
        {
            Random rand = new Random();
            return rand.Next(1, 101) <= 90;
        }

        private async Task<int> GetRepairStaff()
        {
            var repairStaffList = await _equipRepo.GetRepairDepartmentStaffAsync();
            if (!repairStaffList.Any())
            {
                _logger.LogWarning("无可用维修人员");
                throw new Exception("无可用维修人员");
            }
            var random = new Random();
            int index = random.Next(repairStaffList.Count);
            return repairStaffList[index].STAFF_ID;
        }

        public async Task<List<string>> GetAvailableActionsAsync(int equipmentId, string operatorId)
        {
            await CheckPermissionAsync(operatorId, 3);

            var equipment = await _equipRepo.GetByIdAsync(equipmentId);
            if (equipment == null) throw new KeyNotFoundException("设备不存在");

            var stateContext = CreateEquipmentStateContext(equipment);
            var actions = stateContext.GetAllowedOperations();

            if (stateContext.CurrentStateName == EquipmentStatus.Running)
            {
                actions.AddRange(equipment.EQUIPMENT_TYPE.ToLower() switch
                {
                    "空调" => AirConditionerActions,
                    "照明" => LightingActions,
                    "电梯" => ElevatorActions,
                    _ => Array.Empty<string>()
                });
            }
            else if (stateContext.CurrentStateName == EquipmentStatus.Standby)
            {
                actions.AddRange(equipment.EQUIPMENT_TYPE.ToLower() switch
                {
                    "空调" => StandbyAirActions,
                    "照明" => StandbyLightActions,
                    "电梯" => StandbyElevatorActions,
                    _ => Array.Empty<string>()
                });
            }

            if (!actions.Any())
            {
                actions.Add($"当前状态 {stateContext.CurrentStateName} 不可操作");
            }

            return actions;
        }

        public async Task<EquipmentOperationResult> OperateEquipmentAsync(EquipmentOperationDto dto)
        {
            await CheckPermissionAsync(dto.OperatorID, 3);

            var equipment = await _equipRepo.GetByIdAsync(dto.EquipmentID);
            if (equipment == null) throw new KeyNotFoundException("未找到该设备");

            string originalStatus = equipment.EQUIPMENT_STATUS;
            var stateContext = CreateEquipmentStateContext(equipment);

            bool simulationSuccess = SimulateDeviceOperation();
            if (!simulationSuccess)
            {
                _equipRepo.LogOperation(dto.EquipmentID, dto.OperatorID, dto.Operation, false, originalStatus, originalStatus);
                _logger.LogWarning("操作失败:设备={EquipmentId},操作员={Operator},操作={Operation}", dto.EquipmentID, dto.OperatorID, dto.Operation);
                return EquipmentOperationResult.CreateFailure("指令发送失败，请重试");
            }

            var operationResult = stateContext.PerformOperation(dto.Operation, equipment.EQUIPMENT_TYPE);

            if (!operationResult.Success)
            {
                _equipRepo.LogOperation(dto.EquipmentID, dto.OperatorID, dto.Operation, false, originalStatus, originalStatus);
                return operationResult;
            }

            equipment.EQUIPMENT_STATUS = stateContext.CurrentStateName;
            _equipRepo.LogOperation(dto.EquipmentID, dto.OperatorID, dto.Operation, true, originalStatus, equipment.EQUIPMENT_STATUS);

            if (operationResult.StatusChanged)
            {
                _logger.LogInformation($"设备{dto.EquipmentID}状态变更:{originalStatus} -> {equipment.EQUIPMENT_STATUS}");
            }

            _equipRepo.Update(equipment);
            await _equipRepo.SaveChangesAsync();

            return operationResult;
        }

        public async Task<(bool Success, string Message, object Data)> CreateRepairOrderAsync(CreateOrderDto dto)
        {
            await CheckPermissionAsync(dto.OperatorID, 3);

            var equipment = await _equipRepo.GetByIdAsync(dto.EquipmentId);
            if (equipment == null) throw new KeyNotFoundException("设备不存在");

            var stateContext = CreateEquipmentStateContext(equipment);
            if (!stateContext.CanCreateRepairOrder())
            {
                return (false, $"设备当前状态 {stateContext.CurrentStateName} 无法创建维修工单", null);
            }

            stateContext.TransitionToState(EquipmentStatus.UnderMaintenance, $"创建维修工单: {dto.FaultDescription}");
            equipment.EQUIPMENT_STATUS = stateContext.CurrentStateName;

            var STAFF_ID = await GetRepairStaff();
            if (STAFF_ID == 0) return (false, "暂无维修人员，无法维修", null);

            var repairStart = DateTime.Now;
            repairStart = new DateTime(repairStart.Year, repairStart.Month, repairStart.Day,
                 repairStart.Hour, repairStart.Minute, repairStart.Second);

            var newOrder = new RepairOrder
            {
                EQUIPMENT_ID = dto.EquipmentId,
                STAFF_ID = STAFF_ID,
                REPAIR_START = repairStart,
                REPAIR_END = default,
                REPAIR_COST = 0
            };

            await _equipRepo.AddRepairOrderAsync(newOrder);
            _equipRepo.Update(equipment);
            await _equipRepo.SaveChangesAsync();

            _logger.LogInformation("设备账号 {EquipmentId}, 因 {FaultDescription} 处于维修中", dto.EquipmentId, dto.FaultDescription);
            return (true, "工单创建成功", new
            {
                equipmentId = newOrder.EQUIPMENT_ID,
                staffId = newOrder.STAFF_ID,
                repairStart = newOrder.REPAIR_START
            });
        }

        public async Task<(bool Success, string Message)> ConfirmRepairAsync(OrderKeyDto dto)
        {
            await CheckPermissionAsync(dto.OperatorID, 2);

            var order = await _equipRepo.GetRepairOrderAsync(dto.EquipmentId, dto.StaffId, dto.RepairStart);
            if (order == null) throw new KeyNotFoundException("工单不存在");
            if (order.REPAIR_END == default) return (false, "工单尚未完成，无法确认");

            var equipment = await _equipRepo.GetByIdAsync(dto.EquipmentId);
            if (equipment == null) throw new KeyNotFoundException("设备不存在");

            var stateContext = CreateEquipmentStateContext(equipment);
            if (stateContext.CurrentStateName != EquipmentStatus.UnderMaintenance)
                return (false, "工单已确认，无需再次操作");

            bool repairSuccess = order.REPAIR_COST > 0;
            stateContext.CompleteRepair(repairSuccess);

            equipment.EQUIPMENT_STATUS = stateContext.CurrentStateName;
            _equipRepo.Update(equipment);
            await _equipRepo.SaveChangesAsync();

            return (true, repairSuccess ? "设备状态已更新为正常运行" : "维修失败，等待再次分配工单");
        }
    }
}
