using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class AreaRepository : BaseRepository<Area>, IAreaRepository
    {
        private readonly ComplexDbContext _complexContext;

        public AreaRepository(ComplexDbContext context) : base(context)
        {
            _complexContext = context;
        }

        // 1. 查询逻辑
        public async Task<IEnumerable<Area>> GetAreasByCategoryAndStatusAsync(string? category, int? isEmpty)
        {
            var query = _complexContext.Areas.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.CATEGORY.ToUpper() == category.ToUpper());
            }

            if (isEmpty.HasValue)
            {
                query = query.Where(a => a.ISEMPTY == isEmpty.Value);
            }

            // 由于使用了 TPT (Table Per Type)，EF Core 会自动 Join 子表
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<RetailArea>> GetRentedAreasByStoreIdAsync(int storeId)
        {
            // 先查 RentStore 表获取 AreaID，再查 RetailArea
            // 或者直接通过 Join/Subquery
            var areaIds = await _complexContext.RentStores
                .Where(rs => rs.STORE_ID == storeId)
                .Select(rs => rs.AREA_ID)
                .ToListAsync();

            if (!areaIds.Any()) return new List<RetailArea>();

            return await _complexContext.RetailAreas
                .Where(a => areaIds.Contains(a.AREA_ID))
                .ToListAsync();
        }

        public async Task<Area?> GetAreaByIdAsync(int id)
        {
            // 在 TPT 模式下，直接 Find 或 FirstOrDefault 会自动加载对应的子类实例
            return await _complexContext.Areas.FindAsync(id);
        }

        // 2. 依赖性检查 (对应 AreasController.DeleteArea)
        public async Task<bool> HasActiveRentAsync(int areaId)
        {
            return await _complexContext.RentStores.AnyAsync(rs => rs.AREA_ID == areaId);
        }

        public async Task<bool> HasActiveEventAsync(int areaId)
        {
            // 检查是否有未取消的活动预约
            return await _complexContext.VenueEventDetails
                .AnyAsync(ved => ved.AREA_ID == areaId && ved.STATUS != "已取消");
        }

        public async Task<bool> HasParkingSpacesAsync(int areaId)
        {
            return await _complexContext.ParkingSpaceDistributions
                .AnyAsync(psd => psd.AREA_ID == areaId);
        }

        // 特定类型添加并安全封装
        public async Task AddRetailAreaAsync(RetailArea area) => await _complexContext.RetailAreas.AddAsync(area);
        public async Task AddEventAreaAsync(EventArea area) => await _complexContext.EventAreas.AddAsync(area);
        public async Task AddParkingLotAsync(ParkingLot area) => await _complexContext.ParkingLots.AddAsync(area);
        public async Task AddOtherAreaAsync(OtherArea area) => await _complexContext.OtherAreas.AddAsync(area);
    }
}