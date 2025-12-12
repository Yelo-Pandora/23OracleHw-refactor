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

        // === 子类型详情查询 ===
        public async Task<RetailArea?> GetRetailAreaDetailAsync(int areaId)
            => await _complexContext.RetailAreas.FindAsync(areaId);

        public async Task<EventArea?> GetEventAreaDetailAsync(int areaId)
            => await _complexContext.EventAreas.FindAsync(areaId);

        public async Task<ParkingLot?> GetParkingLotDetailAsync(int areaId)
            => await _complexContext.ParkingLots.FindAsync(areaId);

        public async Task<OtherArea?> GetOtherAreaDetailAsync(int areaId)
            => await _complexContext.OtherAreas.FindAsync(areaId);


        // === 子类型更新/插入 (CRUD 核心) ===
        public async Task UpsertRetailAreaAsync(int areaId, string status, double baseRent)
        {
            var localEntity = _complexContext.RetailAreas.Local
            .FirstOrDefault(r => r.AREA_ID == areaId);

            if (localEntity != null)
            {
                _complexContext.Entry(localEntity).State = EntityState.Detached;
            }

            var count = await _complexContext.RetailAreas
                .AsNoTracking()  // 添加 AsNoTracking
                .CountAsync(r => r.AREA_ID == areaId);
            if (count == 0)
            {
                await _complexContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO RETAIL_AREA (AREA_ID, RENT_STATUS, BASE_RENT) VALUES ({areaId}, {status}, {baseRent})");
            }
            else
            {
                await _complexContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE RETAIL_AREA SET RENT_STATUS = {status}, BASE_RENT = {baseRent} WHERE AREA_ID = {areaId}");
            }
        }

        public async Task UpsertEventAreaAsync(int areaId, int capacity, double areaFee)
        {
            var count = await _complexContext.EventAreas
            .AsNoTracking()  // 添加 AsNoTracking
            .CountAsync(e => e.AREA_ID == areaId);
            if (count == 0)
            {
                await _complexContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO EVENT_AREA (AREA_ID, CAPACITY, AREA_FEE) VALUES ({areaId}, {capacity}, {areaFee})");
            }
            else
            {
                var entity = new EventArea { AREA_ID = areaId, CAPACITY = capacity, AREA_FEE = (int)areaFee };
                _complexContext.EventAreas.Attach(entity);
                _complexContext.Entry(entity).Property(x => x.CAPACITY).IsModified = true;
                _complexContext.Entry(entity).Property(x => x.AREA_FEE).IsModified = true;
                await _complexContext.SaveChangesAsync();
            }
        }

        public async Task UpsertParkingLotAsync(int areaId, string status, double parkingFee)
        {
            var count = await _complexContext.ParkingLots
                .AsNoTracking()  // 添加 AsNoTracking
                .CountAsync(p => p.AREA_ID == areaId);
            if (count == 0)
            {
                await _complexContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO PARKING_LOT (AREA_ID, PARKING_FEE) VALUES ({areaId}, {parkingFee})");
            }
            else
            {
                var entity = new ParkingLot { AREA_ID = areaId, PARKING_FEE = (int)parkingFee };
                _complexContext.ParkingLots.Attach(entity);
                _complexContext.Entry(entity).Property(x => x.PARKING_FEE).IsModified = true;
                await _complexContext.SaveChangesAsync();
            }
        }

        public async Task UpsertOtherAreaAsync(int areaId, string type)
        {
            var count = await _complexContext.OtherAreas
                .AsNoTracking()  // 添加 AsNoTracking
                .CountAsync(o => o.AREA_ID == areaId);
            if (count == 0)
            {
                await _complexContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO OTHER_AREA (AREA_ID, TYPE) VALUES ({areaId}, {type})");
            }
            else
            {
                var entity = new OtherArea { AREA_ID = areaId, TYPE = type };
                _complexContext.OtherAreas.Attach(entity);
                _complexContext.Entry(entity).Property(x => x.TYPE).IsModified = true;
                await _complexContext.SaveChangesAsync();
            }
        }

        // === 删除校验 ===
        public async Task<bool> HasRetailDependencyAsync(int areaId)
            => await _complexContext.RentStores.AnyAsync(rs => rs.AREA_ID == areaId);

        public async Task<bool> HasEventDependencyAsync(int areaId)
            => await _complexContext.VenueEventDetails.AnyAsync(ved => ved.AREA_ID == areaId);

        public async Task<bool> HasParkingDependencyAsync(int areaId)
            => await _complexContext.ParkingSpaceDistributions.AnyAsync(psd => psd.AREA_ID == areaId);
    }
}