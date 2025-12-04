using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class EquipmentRepository : BaseRepository<Equipment>, IEquipmentRepository
    {
        // 强类型引用具体的 EquipmentDbContext
        private readonly EquipmentDbContext _equipmentContext;

        public EquipmentRepository(EquipmentDbContext context) : base(context)
        {
            _equipmentContext = context;
        }

        // 封装 EquipmentDbContext 原有方法
        public void LogOperation(int equipmentId, string operatorId, string operationType, bool isSuccess, string prevState, string currState)
        {
            // 直接调用 DbContext 中的内存日志方法
            _equipmentContext.LogOperation(equipmentId, operatorId, operationType, isSuccess, prevState, currState);
        }

        public async Task<List<Equipment>> GetFaultedEquipmentAsync()
        {
            return await _equipmentContext.GetFaultedEquipment();
        }

        public async Task<List<Equipment>> GetEquipmentForMaintenanceAsync()
        {
            return await _equipmentContext.GetEquipmentForMaintenance();
        }

        // 设备位置 (EquipmentLocation) 逻辑
        public async Task<EquipmentLocation?> GetLocationByEquipmentIdAsync(int equipmentId)
        {
            return await _equipmentContext.EquipmentLocations
                .FirstOrDefaultAsync(el => el.EQUIPMENT_ID == equipmentId);
        }

        public async Task AddLocationAsync(EquipmentLocation location)
        {
            await _equipmentContext.EquipmentLocations.AddAsync(location);
        }

        public void RemoveLocation(EquipmentLocation location)
        {
            _equipmentContext.EquipmentLocations.Remove(location);
        }

        public async Task<bool> LocationExistsAsync(int equipmentId)
        {
            return await _equipmentContext.EquipmentLocations
                .AnyAsync(el => el.EQUIPMENT_ID == equipmentId);
        }

        public async Task<Dictionary<int, int>> GetAllEquipmentAreaIdsAsync()
        {
            // 返回 EquipmentID -> AreaID 的映射，优化列表查询性能
            return await _equipmentContext.EquipmentLocations
                .ToDictionaryAsync(el => el.EQUIPMENT_ID, el => el.AREA_ID);
        }

        // 维修工单 (RepairOrder) 逻辑
        public async Task AddRepairOrderAsync(RepairOrder order)
        {
            await _equipmentContext.RepairOrders.AddAsync(order);
        }

        public async Task<RepairOrder?> GetRepairOrderAsync(int equipmentId, int staffId, DateTime repairStart)
        {
            return await _equipmentContext.RepairOrders.FindAsync(equipmentId, staffId, repairStart);
        }

        public async Task<List<RepairOrder>> GetRepairOrdersByEquipmentIdAsync(int equipmentId, bool inProgressOnly)
        {
            var query = _equipmentContext.RepairOrders.Where(r => r.EQUIPMENT_ID == equipmentId);

            if (inProgressOnly)
            {
                query = query.Where(r => r.REPAIR_END == default(DateTime));
            }

            return await query.ToListAsync();
        }

        // 辅助查询 (Area & Staff)
        public async Task<bool> AreaExistsAsync(int areaId)
        {
            return await _equipmentContext.Areas.AnyAsync(a => a.AREA_ID == areaId);
        }

        public async Task<List<Staff>> GetRepairDepartmentStaffAsync()
        {
            // 专门用于 Controller 中的 GetRepairStaff 逻辑
            return await _equipmentContext.Staffs
                .Where(s => s.STAFF_APARTMENT == "维修部")
                .ToListAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            return await _equipmentContext.Staffs
                .FirstOrDefaultAsync(s => s.STAFF_ID == staffId);
        }
    }
}