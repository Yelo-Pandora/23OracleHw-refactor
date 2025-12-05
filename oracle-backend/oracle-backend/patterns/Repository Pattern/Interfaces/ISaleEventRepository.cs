using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface ISaleEventRepository : IRepository<SaleEvent>
    {
        // 核心查询
        
        // 获取当前最大的 EventID (用于 Service 层手动生成自增ID)
        Task<int> GetMaxEventIdAsync();

        // 关联操作
        // 检查/获取特定的关联记录
        Task<PartStore?> GetPartStoreAsync(int eventId, int storeId);

        // 添加关联
        Task AddPartStoreAsync(PartStore partStore);

        // 移除关联
        void RemovePartStore(PartStore partStore);

        // 关联查询
        // 获取某活动下的所有商铺
        Task<List<Store>> GetStoresByEventIdAsync(int eventId);

        // 获取某商铺参与的所有活动
        Task<List<SaleEvent>> GetEventsByStoreIdAsync(int storeId);
    }
}