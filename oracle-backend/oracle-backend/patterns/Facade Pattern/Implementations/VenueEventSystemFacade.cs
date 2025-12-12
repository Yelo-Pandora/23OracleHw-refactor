// 外观模式：场地活动系统具体实现类
// VenueEventSystemFacade.cs

using Microsoft.Extensions.Logging;
using oracle_backend.Models;
using oracle_backend.patterns.Facade_Pattern.Interfaces;
using oracle_backend.Patterns.Factory.Interfaces;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.State.VenueEvent;
using static oracle_backend.Controllers.VenueEventController;

namespace oracle_backend.patterns.Facade_Pattern.Implementations
{
    public class VenueEventSystemFacade : IVenueEventSystemFacade
    {
        private readonly IVenueEventRepository _venueRepo;
        private readonly IVenueEventFactory _venueFactory;
        private readonly ILogger<VenueEventSystemFacade> _logger;

        public VenueEventSystemFacade(
            IVenueEventRepository venueRepo,
            IVenueEventFactory venueFactory,
            ILogger<VenueEventSystemFacade> logger)
        {
            _venueRepo = venueRepo;
            _venueFactory = venueFactory;
            _logger = logger;
        }

        private VenueEventStateContext CreateVenueEventStateContext(VenueEventDetail eventDetail)
        {
            return new VenueEventStateContext(
                eventDetail.EVENT_ID,
                eventDetail.STATUS,
                _logger
            );
        }

        public async Task ApproveReservationAsync(int eventId)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null)
            {
                throw new KeyNotFoundException("找不到对应的预约记录");
            }

            // 使用状态模式处理审批
            var stateContext = CreateVenueEventStateContext(venueEventDetail);
            stateContext.Approve();

            venueEventDetail.STATUS = stateContext.CurrentStateName;
            await _venueRepo.SaveChangesAsync();
        }

        public async Task RejectReservationAsync(int eventId, string reason)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) throw new KeyNotFoundException("找不到对应的预约记录");

            // 使用状态模式处理驳回
            var stateContext = CreateVenueEventStateContext(venueEventDetail);
            stateContext.Reject(reason ?? "未提供原因");

            venueEventDetail.STATUS = stateContext.CurrentStateName;
            await _venueRepo.SaveChangesAsync();
        }

        public async Task CancelEventAsync(int eventId)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) throw new KeyNotFoundException("找不到对应的活动记录");

            // 使用状态模式处理取消
            var stateContext = CreateVenueEventStateContext(venueEventDetail);
            stateContext.Cancel();

            venueEventDetail.STATUS = stateContext.CurrentStateName;
            await _venueRepo.SaveChangesAsync();
        }

        public async Task UpdateEventAsync(int eventId, VenueEventUpdateDto dto)
        {
            var venueEvent = await _venueRepo.GetByIdAsync(eventId);
            if (venueEvent == null) throw new KeyNotFoundException("找不到对应的活动记录");

            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);
            if (venueEventDetail == null) throw new KeyNotFoundException("找不到对应的活动详情记录");

            // 使用状态模式检查是否可以修改
            var stateContext = CreateVenueEventStateContext(venueEventDetail);
            if (!stateContext.CanModify())
            {
                throw new InvalidOperationException($"活动当前状态 {stateContext.CurrentStateName} 不允许修改");
            }

            // 更新字段
            if (!string.IsNullOrEmpty(dto.EventName)) venueEvent.EVENT_NAME = dto.EventName;
            if (dto.Headcount.HasValue) venueEvent.HEADCOUNT = dto.Headcount.Value;

            // 如果要更新状态,使用状态模式验证
            if (!string.IsNullOrEmpty(dto.Status) && dto.Status != venueEventDetail.STATUS)
            {
                stateContext.TransitionToState(dto.Status, "手动更新状态");
                venueEventDetail.STATUS = stateContext.CurrentStateName;
            }

            // 处理参与人员 (批量导入)
            if (dto.ParticipantAccounts != null && dto.ParticipantAccounts.Any())
            {
                await _venueRepo.RemoveTempAuthoritiesByEventIdAsync(eventId);

                foreach (var account in dto.ParticipantAccounts)
                {
                    var tempAuthority = _venueFactory.CreateTempAuthority(account, eventId, 3);
                    await _venueRepo.AddTempAuthorityAsync(tempAuthority);
                }
            }

            _venueRepo.Update(venueEvent);
            await _venueRepo.SaveChangesAsync();
        }

        public async Task<object> SettleEventAsync(int eventId, VenueEventSettlementDto dto)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) throw new KeyNotFoundException("找不到对应的活动记录");

            // 使用状态模式检查是否可以结算
            var stateContext = CreateVenueEventStateContext(venueEventDetail);
            if (!stateContext.CanSettle())
            {
                throw new InvalidOperationException($"活动当前状态 {stateContext.CurrentStateName} 不允许结算");
            }

            var rentHours = (venueEventDetail.RENT_END - venueEventDetail.RENT_START).TotalHours;
            var totalFee = dto.VenueFee + (dto.AdditionalServiceFee ?? 0);

            var settlementInfo = new
            {
                EventId = eventId,
                EventName = venueEventDetail.venueEventNavigation.EVENT_NAME,
                AreaId = venueEventDetail.AREA_ID,
                RentStart = venueEventDetail.RENT_START,
                RentEnd = venueEventDetail.RENT_END,
                RentHours = Math.Round(rentHours, 2),
                VenueFee = dto.VenueFee,
                AdditionalServiceFee = dto.AdditionalServiceFee ?? 0,
                TotalFee = totalFee,
                PaymentMethod = dto.PaymentMethod,
                InvoiceInfo = dto.InvoiceInfo,
                SettlementTime = DateTime.Now
            };

            venueEventDetail.STATUS = "已结算";
            venueEventDetail.FUNDING = totalFee;
            await _venueRepo.SaveChangesAsync();

            return settlementInfo;
        }
    }
}
