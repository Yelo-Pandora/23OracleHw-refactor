using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IAreaRepository : IRepository<Area>
    {
        // --- 1. 查询逻辑 ---
        // 根据类别和空置状态筛选
        Task<IEnumerable<Area>> GetAreasByCategoryAndStatusAsync(string? category, int? isEmpty);

        // 获取单个区域的详细信息（尝试加载子类特有属性）
        // 虽然 EF Core TPT 会自动加载，但为了显式支持 Controller 的 DTO 转换，我们保留此接口
        Task<Area?> GetAreaByIdAsync(int id);
        Task<IEnumerable<RetailArea>> GetRentedAreasByStoreIdAsync(int storeId);

        // 依赖性检查
        // 检查是否被商铺租用
        Task<bool> HasActiveRentAsync(int areaId);
        // 检查是否有关联的场地活动
        Task<bool> HasActiveEventAsync(int areaId);
        // 检查是否有停车位分布
        Task<bool> HasParkingSpacesAsync(int areaId);

        // 添加特定类型的区域 (虽然 BaseRepo.Add 也能做，但显式定义更清晰)
        Task AddRetailAreaAsync(RetailArea area);
        Task AddEventAreaAsync(EventArea area);
        Task AddParkingLotAsync(ParkingLot area);
        Task AddOtherAreaAsync(OtherArea area);

        // 子类型详情查询
        Task<RetailArea?> GetRetailAreaDetailAsync(int areaId);
        Task<EventArea?> GetEventAreaDetailAsync(int areaId);
        Task<ParkingLot?> GetParkingLotDetailAsync(int areaId);
        Task<OtherArea?> GetOtherAreaDetailAsync(int areaId);

        // 子类型更新/插入
        Task UpsertRetailAreaAsync(int areaId, string status, double baseRent);
        Task UpsertEventAreaAsync(int areaId, int capacity, double areaFee);
        Task UpsertParkingLotAsync(int areaId, string status, double parkingFee);
        Task UpsertOtherAreaAsync(int areaId, string type);

        // 删除校验
        Task<bool> HasRetailDependencyAsync(int areaId); // 检查 RentStore
        Task<bool> HasEventDependencyAsync(int areaId);  // 检查 VenueEvent
        Task<bool> HasParkingDependencyAsync(int areaId); // 检查 ParkingSpaceDistribution
    }
}