using Microsoft.AspNetCore.Mvc;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.State.Equipment;
using System.ComponentModel.DataAnnotations;
using oracle_backend.patterns.Facade_Pattern.Interfaces;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        // [Repository Pattern] 数据访问层接口
        private readonly IEquipmentRepository _equipRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ILogger<EquipmentController> _logger;
        
        // [Facade Pattern] 设备系统外观接口
        private readonly IEquipmentSystemFacade _equipFacade;

        public EquipmentController(
            IEquipmentRepository equipRepo,
            IAccountRepository accountRepo,
            ILogger<EquipmentController> logger,
            IEquipmentSystemFacade equipFacade)
        {
            _equipRepo = equipRepo;
            _accountRepo = accountRepo;
            _logger = logger;
            _equipFacade = equipFacade;
        }

        /// <summary>
        /// [State Pattern] 创建设备状态上下文
        /// </summary>
        private EquipmentStateContext CreateEquipmentStateContext(Equipment equipment)
        {
            return new EquipmentStateContext(
                equipment.EQUIPMENT_ID,
                equipment.EQUIPMENT_TYPE,
                equipment.EQUIPMENT_STATUS,
                _logger
            );
        }

        // 2.9.1 查看设备列表
        public class EquipmentListDto
        {
            public int equipment_ID { get; set; }
            public string equipment_TYPE { get; set; }
            public string equipment_STATUS { get; set; }
            public int? area_ID { get; set; } 
        }

        [HttpGet("EquipmentList")]
        public async Task<ActionResult<IEnumerable<EquipmentListDto>>> GetEquipmentList([FromQuery] string OperatorID)
        {
            _logger.LogInformation("正在读取设备列表信息");

            try
            {
                // [Repository Pattern] 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑：查询设备列表
                var equipments = await _equipRepo.GetAllAsync();
                
                // 优化：一次性获取所有位置映射
                var locationMap = await _equipRepo.GetAllEquipmentAreaIdsAsync();

                var list = equipments.Select(e => new EquipmentListDto
                {
                    equipment_ID = e.EQUIPMENT_ID,
                    equipment_TYPE = e.EQUIPMENT_TYPE,
                    equipment_STATUS = e.EQUIPMENT_STATUS,
                    area_ID = locationMap.ContainsKey(e.EQUIPMENT_ID) ? locationMap[e.EQUIPMENT_ID] : 0
                }).ToList();

                if (!list.Any())
                    return NotFound("不存在任何设备");

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取设备列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 查询具体设备
        public class EquipmentDetailDto
        {
            public int EQUIPMENT_ID { get; set; }
            public string EQUIPMENT_TYPE { get; set; }
            public string EQUIPMENT_STATUS { get; set; }
            public string? PORT { get; set; }
            public int? EQUIPMENT_COST { get; set; }
            public DateTime BUY_TIME { get; set; }
            public int AREA_ID { get; set; }
        }

        [HttpGet("EquipmentDetail")]
        public async Task<ActionResult<EquipmentDetailDto>> GetEquipmentDetail([FromQuery] int equipmentID, [FromQuery] string OperatorID)
        {
            _logger.LogInformation("正在查询设备信息");
            try
            {
                _logger.LogInformation($"收到 OperatorID = {OperatorID}");

                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑
                var equipment = await _equipRepo.GetByIdAsync(equipmentID);
                if (equipment == null)
                    return NotFound("未找到该设备");

                if (equipment.EQUIPMENT_STATUS == EquipmentStatus.Offline)
                    return BadRequest("该设备离线，无法获取状态");

                var location = await _equipRepo.GetLocationByEquipmentIdAsync(equipment.EQUIPMENT_ID);

                var dto = new EquipmentDetailDto
                {
                    EQUIPMENT_ID = equipment.EQUIPMENT_ID,
                    EQUIPMENT_TYPE = equipment.EQUIPMENT_TYPE,
                    EQUIPMENT_STATUS = equipment.EQUIPMENT_STATUS,
                    EQUIPMENT_COST = equipment.EQUIPMENT_COST,
                    PORT = equipment.PORT,
                    BUY_TIME = equipment.BUY_TIME,
                    AREA_ID = location?.AREA_ID ?? 0
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取设备信息失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 2.9.2 设备状态常量
        public static class EquipmentStatus
        {
            public const string Running = "运行中";
            public const string Faulted = "故障";
            public const string Offline = "离线";
            public const string UnderMaintenance = "维修中";
            public const string Standby = "待机";
            public const string Discarded = "废弃";
        }

        private static readonly string[] AirConditionerActions = { "关机", "制冷模式", "制热模式", "调节温度", "紧急停止" };
        private static readonly string[] LightingActions = { "关灯", "调亮", "调暗", "紧急停止" };
        private static readonly string[] ElevatorActions = { "停止", "开门", "关门", "紧急停止" };
        private static readonly string[] StandbyAirActions = { "开机", "紧急停止" };
        private static readonly string[] StandbyLightActions = { "开灯", "紧急停止" };
        private static readonly string[] StandbyElevatorActions = { "启动", "紧急停止" };

        // [State Pattern] & [Facade Pattern] 使用外观模式封装状态模式逻辑来获取可用操作
        [HttpGet("ActionsList")]
        public async Task<IActionResult> GetAvailableActions([FromQuery] int id, [FromQuery] string OperatorID)
        {
            _logger.LogInformation($"正在加载设备 {id} 的可操作列表");
            
            try
            {
                var actions = await _equipFacade.GetAvailableActionsAsync(id, OperatorID);
                return Ok(actions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示设备 {id} 可操作列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        public class EquipmentOperationDto
        {
            public int EquipmentID { get; set; }
            public string OperatorID { get; set; }
            public string Operation { get; set; }
        }

        // [State Pattern] & [Facade Pattern] 使用外观模式操作设备（内部处理状态转换）
        [HttpPost("operate")]
        public async Task<IActionResult> OperateEquipment([FromBody] EquipmentOperationDto dto)
        {
            _logger.LogInformation($"正在操作设备{dto.EquipmentID}:{dto.Operation}");
            
            try
            {
                var result = await _equipFacade.OperateEquipmentAsync(dto);

                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                return Ok(new
                {
                    status = result.NewStatus,
                    result = result.Message,
                    statusChanged = result.StatusChanged
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作设备 {dto.EquipmentID} 失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        private string MapOperationToStatus(string operation, string currentStatus, string equipmentType)
        {
            operation = operation.ToLower();
            equipmentType = equipmentType.ToLower();

            switch (operation)
            {
                case "开机" when equipmentType == "空调":
                case "开灯" when equipmentType == "照明":
                case "启动" when equipmentType == "电梯":
                    return EquipmentStatus.Running;

                case "关机" when equipmentType == "空调":
                case "关灯" when equipmentType == "照明":
                case "停止" when equipmentType == "电梯":
                    return EquipmentStatus.Standby;
            }
            return currentStatus;
        }

        private bool IsOperationValidForStatus(string operation, string currentStatus, string equipmentType)
        {
            if (currentStatus == EquipmentStatus.Offline ||
                     currentStatus == EquipmentStatus.UnderMaintenance ||
                     currentStatus == EquipmentStatus.Faulted)
                return false;

            operation = operation.ToLower();

            if (currentStatus == EquipmentStatus.Running)
            {
                if (operation == "开机" || operation == "开灯" || operation == "启动")
                    return false;
            }
            else if (currentStatus == EquipmentStatus.Standby)
            {
                if (operation == "关机" || operation == "关灯" || operation == "停止")
                    return false;
            }
            return true;
        }

        private bool SimulateDeviceOperation()
        {
            Random rand = new Random();
            return rand.Next(1, 101) <= 90;
        }

        public class RepairOrderDto
        {
            public string OperatorID { get; set; }
            public int EquipmentID { get; set; }
            public bool inProgressOnly { get; set; }
        }
        public class RepairOrderDetailDto
        {
            public int EQUIPMENT_ID { get; set; }
            public int STAFF_ID { get; set; }
            public DateTime REPAIR_START { get; set; }
            public DateTime REPAIR_END { get; set; }
            public double REPAIR_COST { get; set; }
        }

        [HttpGet("RepairList")]
        public async Task<ActionResult<IEnumerable<RepairOrderDetailDto>>> GetRepairList([FromQuery] RepairOrderDto dto)
        {
            _logger.LogInformation("正在查询工单列表");
            try
            {
                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(dto.OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(dto.OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(dto.OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑
                var equipment = await _equipRepo.GetByIdAsync(dto.EquipmentID);
                if (equipment == null)
                    return NotFound($"设备ID={dto.EquipmentID} 不存在");

                var repairOrders = await _equipRepo.GetRepairOrdersByEquipmentIdAsync(dto.EquipmentID, dto.inProgressOnly);

                var resultList = repairOrders.Select(r => new RepairOrderDetailDto
                {
                    EQUIPMENT_ID = r.EQUIPMENT_ID,
                    STAFF_ID = r.STAFF_ID,
                    REPAIR_START = r.REPAIR_START,
                    REPAIR_END = r.REPAIR_END,
                    REPAIR_COST = r.REPAIR_COST
                }).ToList();

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取工单列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 2.9.3 创建工单
        public class CreateOrderDto
        {
            public string OperatorID { get; set; }
            public int EquipmentId { get; set; }
            public string FaultDescription { get; set; }
        }

        public class CompleteRepairDto
        {
            public int EquipmentId { get; set; }
            public int StaffId { get; set; }
            public DateTime RepairStart { get; set; }
            public double Cost { get; set; }
            public bool Success { get; set; }
        }

        public class OrderKeyDto
        {
            public string OperatorID { get; set; }
            public int EquipmentId { get; set; }
            public int StaffId { get; set; }
            public DateTime RepairStart { get; set; }
        }

        // [Facade Pattern] 使用外观模式创建维修工单（内部可能涉及状态变更）
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("正在创建工单");
            
            try
            {
                var result = await _equipFacade.CreateRepairOrderAsync(dto);

                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                // result.Data 包含了 compositeKey 所需的匿名对象结构
                return Ok(new
                {
                    message = result.Message,
                    compositeKey = result.Data
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工单创建失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpPut("CompleteRepair")]
        public async Task<IActionResult> CompleteRepair([FromBody] CompleteRepairDto dto)
        {
            _logger.LogInformation("正在更新工单");
            try
            {
                var order = await _equipRepo.GetRepairOrderAsync(
                    dto.EquipmentId, dto.StaffId, dto.RepairStart);

                if (order == null)
                    return NotFound("工单不存在");

                if (order.REPAIR_END != default)
                    return BadRequest("工单已完成，不可修改");
                if (dto.Cost < 0)
                    return BadRequest("维修费用不可为负数");

                order.REPAIR_END = DateTime.Now;
                order.REPAIR_COST = dto.Success ? Math.Abs(dto.Cost) : -Math.Abs(dto.Cost);

                await _equipRepo.SaveChangesAsync();
                return Ok(dto.Success ? "维修成功结果已提交" : "维修失败结果已提交");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成维修工单失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // [Facade Pattern] 使用外观模式确认维修结果
        [HttpPost("confirm-repair")]
        public async Task<IActionResult> ConfirmRepair([FromBody] OrderKeyDto dto)
        {
            
            try
            {
                var result = await _equipFacade.ConfirmRepairAsync(dto);

                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                return Ok(result.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认维修失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        private async Task<int> GetRepairStaff()
        {
            try
            {
                // [Repository Pattern] 获取维修部员工列表
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "随机选择维修人员失败");
                throw;
            }
        }

        // 新增添加、删除设备功能

        [HttpPost("AddEquipment")]
        public async Task<ActionResult<Equipment>> AddEquipment([FromBody] Equipment newEquipment, string OperatorID)
        {
            _logger.LogInformation("正在添加设备");
            try
            {
                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                Console.WriteLine(">>> 权限检查结束，未触发拒绝");

                var equipment = await _equipRepo.GetByIdAsync(newEquipment.EQUIPMENT_ID);
                if (equipment != null)
                    return BadRequest("该设备ID已存在，无法新增设备");

                await _equipRepo.AddAsync(newEquipment);
                await _equipRepo.SaveChangesAsync();

                _logger.LogInformation($"设备添加成功，ID: {newEquipment.EQUIPMENT_ID}");
                return CreatedAtAction(
                    nameof(GetEquipmentDetail),
                    new { equipmentID = newEquipment.EQUIPMENT_ID },
                    new
                    {
                        message = "设备添加成功",
                        equipment = newEquipment
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备添加失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpPost("AddEquipmentLocation")]
        public async Task<ActionResult<EquipmentLocation>> AddEquipmentLocation(int equipmentID, int areaID, string OperatorID)
        {
            _logger.LogInformation("正在添加设备位置信息");
            try
            {
                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑
                var equipment = await _equipRepo.GetByIdAsync(equipmentID);
                if (equipment == null)
                    return BadRequest("该设备ID不存在，无法添加设备位置信息");

                if (equipment.EQUIPMENT_STATUS == EquipmentStatus.Discarded)
                    return BadRequest("该设备已被弃用");

                // 检查设备位置是否已经存在
                if (await _equipRepo.LocationExistsAsync(equipmentID))
                    return BadRequest("该设备已经存在位置信息，无法重复添加");

                if (!await _equipRepo.AreaExistsAsync(areaID))
                    return BadRequest($"区域ID {areaID} 不存在，无法添加设备位置信息");

                var newLocation = new EquipmentLocation
                {
                    EQUIPMENT_ID = equipmentID,
                    AREA_ID = areaID
                };

                await _equipRepo.AddLocationAsync(newLocation);
                await _equipRepo.SaveChangesAsync();

                _logger.LogInformation($"设备位置添加成功，设备ID: {equipmentID}, 区域ID: {areaID}");
                return Created("GetEquipmentLocation", new
                {
                    message = "设备位置添加成功",
                    equipmentID = newLocation.EQUIPMENT_ID,
                    areaID = newLocation.AREA_ID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备位置信息添加失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpDelete("DeleteEquipment")]
        public async Task<IActionResult> DeleteEquipment(int equipmentID, string OperatorID)
        {
            _logger.LogInformation($"正在尝试删除设备ID={equipmentID}");
            try
            {
                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑
                var equipment = await _equipRepo.GetByIdAsync(equipmentID);
                if (equipment == null)
                    return NotFound("设备不存在");

                var location = await _equipRepo.GetLocationByEquipmentIdAsync(equipmentID);
                if (location != null)
                {
                    _equipRepo.RemoveLocation(location);
                    _logger.LogInformation($"删除关联设备位置，设备ID: {equipmentID}");
                }

                equipment.EQUIPMENT_STATUS = EquipmentStatus.Discarded;
                _equipRepo.Update(equipment); // 更新状态
                await _equipRepo.SaveChangesAsync();

                _logger.LogInformation($"设备删除成功，ID: {equipmentID}");
                return Ok(new { message = $"设备删除成功，ID: {equipmentID}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpDelete("UnbindEquipmentLocation")]
        public async Task<IActionResult> UnbindEquipmentLocation(int equipmentID, string OperatorID)
        {
            _logger.LogInformation($"正在尝试解绑设备位置，设备ID={equipmentID}");
            try
            {
                // 权限校验逻辑
                var isAuthority = await _accountRepo.CheckAuthority(OperatorID, 3);
                if (!isAuthority)
                {
                    return BadRequest("操作者权限不足");
                }

                var operatorAccount = await _accountRepo.FindAccountByUsername(OperatorID);
                if (operatorAccount != null && operatorAccount.AUTHORITY == 3)
                {
                    var staffAccount = await _accountRepo.CheckStaff(OperatorID);
                    if (staffAccount == null)
                        return BadRequest("该操作员无对应员工");

                    var staff = await _equipRepo.GetStaffByIdAsync(staffAccount.STAFF_ID);

                    if (staff == null)
                        return BadRequest("不存在该员工");

                    if (staff.STAFF_APARTMENT != "维修部")
                        return BadRequest("该员工非维修部员工无权操作设备");
                }

                // [Repository Pattern] 业务逻辑
                var location = await _equipRepo.GetLocationByEquipmentIdAsync(equipmentID);
                if (location == null)
                    return NotFound("设备位置不存在");

                _equipRepo.RemoveLocation(location);
                await _equipRepo.SaveChangesAsync();

                _logger.LogInformation($"设备位置解绑成功，设备ID: {equipmentID}");
                return Ok(new { message = $"设备位置解绑成功，设备ID: {equipmentID}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解绑设备位置失败");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}