using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class SaleEventRepository : BaseRepository<SaleEvent>, ISaleEventRepository
    {
        private readonly SaleEventDbContext _saleEventContext;

        public SaleEventRepository(SaleEventDbContext context) : base(context)
        {
            _saleEventContext = context;
        }

        // 获取最大ID，如果表为空返回0
        public async Task<int> GetMaxEventIdAsync()
        {
            var maxId = await _saleEventContext.SaleEvents
                .OrderByDescending(e => e.EVENT_ID)
                .Select(e => e.EVENT_ID)
                .FirstOrDefaultAsync();

            return maxId;
        }

        // PartStore 关联操作
        public async Task<PartStore?> GetPartStoreAsync(int eventId, int storeId)
        {
            return await _saleEventContext.PartStores
                .FirstOrDefaultAsync(ps => ps.EVENT_ID == eventId && ps.STORE_ID == storeId);
        }

        public async Task AddPartStoreAsync(PartStore partStore)
        {
            await _saleEventContext.PartStores.AddAsync(partStore);
        }

        public void RemovePartStore(PartStore partStore)
        {
            _saleEventContext.PartStores.Remove(partStore);
        }

        // 关联查询：通过中间表筛选
        public async Task<List<Store>> GetStoresByEventIdAsync(int eventId)
        {
            return await _saleEventContext.PartStores
                .Where(ps => ps.EVENT_ID == eventId)
                .Select(ps => ps.storeNavigation) // 利用导航属性
                .ToListAsync();
        }

        public async Task<List<SaleEvent>> GetEventsByStoreIdAsync(int storeId)
        {
            return await _saleEventContext.PartStores
                .Where(ps => ps.STORE_ID == storeId)
                .Select(ps => ps.saleEventNavigation) // 利用导航属性
                .ToListAsync();
        }
    }
}