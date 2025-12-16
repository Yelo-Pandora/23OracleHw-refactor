// 场地活动状态接口
// Refactored with State Pattern

using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 场地活动状态接口
    /// </summary>
    public interface IVenueEventState : IState<VenueEventStateContext>
    {
        /// <summary>
        /// 审批通过
        /// </summary>
        /// <param name="context">活动上下文</param>
        void Approve(VenueEventStateContext context);

        /// <summary>
        /// 审批驳回
        /// </summary>
        /// <param name="context">活动上下文</param>
        /// <param name="reason">驳回原因</param>
        void Reject(VenueEventStateContext context, string reason);

        /// <summary>
        /// 开始活动
        /// </summary>
        /// <param name="context">活动上下文</param>
        void Start(VenueEventStateContext context);

        /// <summary>
        /// 结束活动
        /// </summary>
        /// <param name="context">活动上下文</param>
        void End(VenueEventStateContext context);

        /// <summary>
        /// 取消活动
        /// </summary>
        /// <param name="context">活动上下文</param>
        void Cancel(VenueEventStateContext context);

        /// <summary>
        /// 判断是否可以修改活动信息
        /// </summary>
        /// <param name="context">活动上下文</param>
        /// <returns>是否可以修改</returns>
        bool CanModify(VenueEventStateContext context);

        /// <summary>
        /// 判断是否可以结算
        /// </summary>
        /// <param name="context">活动上下文</param>
        /// <returns>是否可以结算</returns>
        bool CanSettle(VenueEventStateContext context);
    }
}

