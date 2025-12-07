// 已通过状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 已通过状态 - 活动预约审批通过
    /// </summary>
    public class ApprovedState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.Approved;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == VenueEventStateContext.StateNames.InProgress => true,  // 可以开始
                var s when s == VenueEventStateContext.StateNames.Cancelled => true,   // 可以取消
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "开始活动",
                "取消活动",
                "修改活动信息"
            };
        }

        public override void Start(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.InProgress, "活动开始");
        }

        public override void Cancel(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Cancelled, "活动取消");
        }

        public override bool CanModify(VenueEventStateContext context)
        {
            return true;
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 审批通过,等待开始");
        }
    }
}

