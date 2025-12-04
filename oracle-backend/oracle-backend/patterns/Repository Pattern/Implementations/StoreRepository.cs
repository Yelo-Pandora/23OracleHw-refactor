using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class StoreRepository : BaseRepository<Store>, IStoreRepository
    {
        private readonly StoreDbContext _storeContext;

        public StoreRepository(StoreDbContext context) : base(context)
        {
            _storeContext = context;
        }

        public async Task<bool> IsAreaAvailable(int areaId)
            => await _storeContext.IsAreaAvailable(areaId);

        public async Task<bool> TenantExists(string tenantName, string contactInfo)
            => await _storeContext.TenantExists(tenantName, contactInfo);

        // 检查区域ID是否存在
        public async Task<bool> AreaIdExists(int areaId)
        {
            return await _storeContext.AREA.AnyAsync(a => a.AREA_ID == areaId);
        }

        public async Task<int> GetNextStoreId()
            => await _storeContext.GetNextStoreId();

        public async Task UpdateAreaStatus(int areaId, bool isEmpty, string rentStatus)
            => await _storeContext.UpdateAreaStatus(areaId, isEmpty, rentStatus);

        public async Task AddRentStore(RentStore rentStore)
            => await _storeContext.RENT_STORE.AddAsync(rentStore);

        // 添加零售区域
        public async Task AddRetailAreaAsync(RetailArea area)
        {
            await _storeContext.RETAIL_AREA.AddAsync(area);
        }

        // 获取可用区域
        public async Task<List<object>> GetAvailableAreas()
        {
            return await _storeContext.GetAvailableAreas();
        }

        // 获取基础统计
        public async Task<BasicStatistics> GetBasicStatistics()
        {
            return await _storeContext.GetBasicStatistics();
        }
    }
}