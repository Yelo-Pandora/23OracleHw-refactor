using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class VenueEventRepository : BaseRepository<VenueEvent>, IVenueEventRepository
    {
        private readonly ComplexDbContext _complexContext;

        public VenueEventRepository(ComplexDbContext context) : base(context)
        {
            _complexContext = context;
        }

        // 封装 Context 方法
        public async Task<Event?> FindEventByIdAsync(int eventId)
        {
            return await _complexContext.FindEventById(eventId);
        }

        // 2. 预约与冲突检查
        public async Task<VenueEventDetail?> GetConflictingReservationAsync(int areaId, DateTime start, DateTime end)
        {
            // 逻辑：同一区域，状态不是已取消，且时间段有重叠
            return await _complexContext.VenueEventDetails
                .Where(ved => ved.AREA_ID == areaId &&
                             ved.STATUS != "已取消" &&
                             ((ved.RENT_START <= start && ved.RENT_END > start) ||
                              (ved.RENT_START < end && ved.RENT_END >= end) ||
                              (ved.RENT_START >= start && ved.RENT_END <= end)))
                .FirstOrDefaultAsync();
        }

        public async Task AddVenueEventDetailAsync(VenueEventDetail detail)
        {
            await _complexContext.VenueEventDetails.AddAsync(detail);
        }

        public async Task<VenueEventDetail?> GetEventDetailAsync(int eventId)
        {
            // 包含所有关联表，用于详情展示或结算
            return await _complexContext.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Include(ved => ved.collaborationNavigation)
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);
        }

        public async Task<VenueEventDetail?> GetEventDetailByKeyAsync(int eventId, int areaId, int collaborationId)
        {
            return await _complexContext.VenueEventDetails
                .FindAsync(eventId, areaId, collaborationId);
        }

        // 报表与列表查询
        public async Task<IEnumerable<VenueEventDetail>> GetVenueEventsWithDetailsAsync(string? status, int? areaId)
        {
            var query = _complexContext.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Include(ved => ved.collaborationNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ved => ved.STATUS == status);
            }

            if (areaId.HasValue)
            {
                query = query.Where(ved => ved.AREA_ID == areaId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<VenueEventDetail>> GetVenueEventsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _complexContext.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Where(ved => ved.RENT_START >= startDate && ved.RENT_END <= endDate)
                .ToListAsync();
        }

        // 临时权限 (TempAuthority) 管理
        public async Task<TempAuthority?> GetTempAuthorityAsync(string account, int eventId)
        {
            return await _complexContext.TempAuthorities
                .FirstOrDefaultAsync(ta => ta.ACCOUNT == account && ta.EVENT_ID == eventId);
        }

        public async Task AddTempAuthorityAsync(TempAuthority tempAuth)
        {
            await _complexContext.TempAuthorities.AddAsync(tempAuth);
        }

        public void RemoveTempAuthority(TempAuthority tempAuth)
        {
            _complexContext.TempAuthorities.Remove(tempAuth);
        }

        public async Task RemoveTempAuthoritiesByEventIdAsync(int eventId)
        {
            var items = await _complexContext.TempAuthorities
                .Where(ta => ta.EVENT_ID == eventId)
                .ToListAsync();

            if (items.Any())
            {
                _complexContext.TempAuthorities.RemoveRange(items);
            }
        }

        public async Task<List<string>> GetParticipantAccountsAsync(int eventId)
        {
            return await _complexContext.TempAuthorities
                .Where(ta => ta.EVENT_ID == eventId)
                .Select(ta => ta.ACCOUNT)
                .ToListAsync();
        }

        // 辅助检查函数
        public async Task<bool> CollaborationExistsAsync(int id)
        {
            return await _complexContext.Collaborations.AnyAsync(c => c.COLLABORATION_ID == id);
        }

        public async Task<EventArea?> GetEventAreaByIdAsync(int id)
        {
            return await _complexContext.EventAreas.FindAsync(id);
        }

        public async Task<IEnumerable<VenueEventDetail>> GetSettledEventsInRangeAsync(DateTime startDate, DateTime endDate, int areaId)
        {
            // 逻辑复刻自 CashFlowController.GetEventSettlementsAsync，并增加 AreaId 过滤
            return await _complexContext.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Include(ved => ved.collaborationNavigation)
                .Where(ved => ved.AREA_ID == areaId &&
                              ved.STATUS == "已结算" &&
                              ved.RENT_END >= startDate &&
                              ved.RENT_START <= endDate)
                .ToListAsync();
        }
    }
}