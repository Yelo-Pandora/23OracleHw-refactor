using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        // --- 1. 封装 EquipmentDbContext 中的自定义方法 ---
        void LogOperation(int equipmentId, string operatorId, string operationType, bool isSuccess, string prevState, string currState);
        Task<List<Equipment>> GetFaultedEquipmentAsync();
        Task<List<Equipment>> GetEquipmentForMaintenanceAsync();

        // --- 2. 设备位置 (EquipmentLocation) 相关操作 ---
        Task<EquipmentLocation?> GetLocationByEquipmentIdAsync(int equipmentId);
        Task AddLocationAsync(EquipmentLocation location);
        void RemoveLocation(EquipmentLocation location);
        Task<bool> LocationExistsAsync(int equipmentId);
        // 获取所有设备及其位置ID (用于 Controller 的 GetEquipmentList)
        Task<Dictionary<int, int>> GetAllEquipmentAreaIdsAsync();

        // --- 3. 维修工单 (RepairOrder) 相关操作 ---
        Task AddRepairOrderAsync(RepairOrder order);
        // 查找特定工单 (复合主键)
        Task<RepairOrder?> GetRepairOrderAsync(int equipmentId, int staffId, DateTime repairStart);
        // 查询某设备的工单列表 (支持筛选进行中)
        Task<List<RepairOrder>> GetRepairOrdersByEquipmentIdAsync(int equipmentId, bool inProgressOnly);

        // --- 4. 辅助查询 (Area & Staff) ---
        Task<bool> AreaExistsAsync(int areaId);
        // 获取维修部的所有员工 (用于随机分配工单)
        Task<List<Staff>> GetRepairDepartmentStaffAsync();
        Task<Staff?> GetStaffByIdAsync(int staffId);
    }
}