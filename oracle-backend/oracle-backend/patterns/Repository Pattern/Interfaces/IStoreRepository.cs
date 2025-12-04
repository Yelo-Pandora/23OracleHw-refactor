using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IStoreRepository : IRepository<Store>
    {
        // 对应 StoreDbContext 中的自定义方法
        Task<bool> IsAreaAvailable(int areaId);
        Task<bool> TenantExists(string tenantName, string contactInfo);
        Task<bool> AreaIdExists(int areaId);

        // ID 生成与状态更新
        Task<int> GetNextStoreId();
        Task UpdateAreaStatus(int areaId, bool isEmpty, string rentStatus);

        // 包含 RentStore 关联操作
        Task AddRentStore(RentStore rentStore);
        Task AddRetailAreaAsync(RetailArea area);

        // 简单查询
        // 获取可用区域列表
        Task<List<object>> GetAvailableAreas();

        // 获取基础统计
        Task<BasicStatistics> GetBasicStatistics();
        Task<List<RentCollectionDetail>> GetRentCollectionDetailsAsync(string period);
    }
}