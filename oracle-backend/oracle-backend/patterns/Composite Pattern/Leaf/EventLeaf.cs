using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.patterns.Composite_Pattern.Component;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    public class EventLeaf : IAreaComponent
    {
        private readonly IAreaRepository _areaRepository;
        private readonly IVenueEventRepository _venueEventRepository; // 注入业务Repo
        private readonly int _areaId;

        public EventLeaf(IAreaRepository areaRepository, IVenueEventRepository venueEventRepository, int areaId)
        {
            _areaRepository = areaRepository;
            _venueEventRepository = venueEventRepository;
            _areaId = areaId;
        }

        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // 使用 VenueEventRepository 的新方法
            var events = await _venueEventRepository.GetSettledEventsInRangeAsync(startDate, endDate, _areaId);

            return events.Select(r => new CashFlowRecord
            {
                Date = r.RENT_END,
                Type = "收入",
                Category = "场地活动",
                Description = $"活动{r.EVENT_ID}结算",
                Amount = r.FUNDING,
                Reference = $"Event-{r.EVENT_ID}",
                RelatedPartyType = "合作方",
                RelatedPartyId = r.COLLABORATION_ID
            });
        }

        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            var eventArea = await _areaRepository.GetEventAreaDetailAsync(_areaId);
            if (area == null) return null;

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "EVENT",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = area.ISEMPTY == 0 ? 1.0 : 0.0,
                Price = eventArea?.AREA_FEE != null ? (double)eventArea.AREA_FEE : null,
                CapacityOrSpaces = eventArea?.CAPACITY,
                BusinessStatus = null,
                SubType = null
            };
        }

        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            if (area != null)
            {
                if (config.IsEmpty.HasValue) area.ISEMPTY = config.IsEmpty.Value;
                if (config.AreaSize.HasValue) area.AREA_SIZE = config.AreaSize.Value;
                _areaRepository.Update(area);
                await _areaRepository.SaveChangesAsync();
            }

            await _areaRepository.UpsertEventAreaAsync(
                _areaId,
                config.Capacity ?? 0,
                config.Price ?? 0);
        }

        public async Task<string?> ValidateDeleteConditionAsync()
        {
            return await _areaRepository.HasEventDependencyAsync(_areaId)
                ? "无法删除：该区域已有关联的场地活动。" : null;
        }
    }
}