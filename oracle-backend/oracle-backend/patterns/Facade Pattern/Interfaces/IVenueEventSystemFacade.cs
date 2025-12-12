// 外观模式：场地活动系统接口
// IVenueEventSystemFacade.cs

using static oracle_backend.Controllers.VenueEventController;


namespace oracle_backend.patterns.Facade_Pattern.Interfaces
{
    /// <summary>
    /// 场地活动系统外观接口
    /// 封装了基于状态模式的活动审批、开始、取消、结算流程
    /// </summary>
    public interface IVenueEventSystemFacade
    {
        /// <summary>
        /// 审批预约 (State: PendingApproval -> Approved)
        /// </summary>
        Task ApproveReservationAsync(int eventId);

        /// <summary>
        /// 驳回预约 (State: PendingApproval -> Rejected)
        /// </summary>
        Task RejectReservationAsync(int eventId, string reason);

        /// <summary>
        /// 取消活动 (State: Multiple -> Cancelled)
        /// </summary>
        Task CancelEventAsync(int eventId);

        /// <summary>
        /// 更新活动信息 (需检查状态是否允许修改)
        /// </summary>
        Task UpdateEventAsync(int eventId, VenueEventUpdateDto dto);

        /// <summary>
        /// 活动结算 (State: Ended -> Settled)
        /// </summary>
        Task<object> SettleEventAsync(int eventId, VenueEventSettlementDto dto);
    }
}
