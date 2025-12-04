using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IVenueEventRepository : IRepository<VenueEvent>
    {
        // ComplexDbContext原有方法
        Task<Event?> FindEventByIdAsync(int eventId);

        // 预约与冲突检查
        // 检查时间段内是否有冲突的预约
        Task<VenueEventDetail?> GetConflictingReservationAsync(int areaId, DateTime start, DateTime end);
        // 添加预约详情
        Task AddVenueEventDetailAsync(VenueEventDetail detail);
        // 获取活动详情 (包含导航属性)
        Task<VenueEventDetail?> GetEventDetailAsync(int eventId);
        // 获取活动详情 (根据主键)
        Task<VenueEventDetail?> GetEventDetailByKeyAsync(int eventId, int areaId, int collaborationId);

        // 报表与列表查询
        Task<IEnumerable<VenueEventDetail>> GetVenueEventsWithDetailsAsync(string? status, int? areaId);
        Task<IEnumerable<VenueEventDetail>> GetVenueEventsInRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<VenueEventDetail>> GetSettledEventsInRangeAsync(DateTime startDate, DateTime endDate, int areaId);

        // 临时权限管理
        // 查找特定的临时权限
        Task<TempAuthority?> GetTempAuthorityAsync(string account, int eventId);
        // 添加临时权限
        Task AddTempAuthorityAsync(TempAuthority tempAuth);
        // 移除临时权限
        void RemoveTempAuthority(TempAuthority tempAuth);
        // 根据 EventID 批量移除 (用于更新活动参与人)
        Task RemoveTempAuthoritiesByEventIdAsync(int eventId);
        // 获取某活动的所有参与者账号
        Task<List<string>> GetParticipantAccountsAsync(int eventId);

        // --- 5. 辅助检查 ---
        Task<bool> CollaborationExistsAsync(int id);
        Task<EventArea?> GetEventAreaByIdAsync(int id);
    }
}