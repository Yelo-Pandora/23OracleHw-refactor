// 待审批状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 待审批状态 - 活动预约等待审批
    /// </summary>
    public class PendingApprovalState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.PendingApproval;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == VenueEventStateContext.StateNames.Approved => true,    // 可以通过
                var s when s == VenueEventStateContext.StateNames.Rejected => true,    // 可以驳回
                var s when s == VenueEventStateContext.StateNames.Cancelled => true,   // 可以取消
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "审批通过",
                "审批驳回",
                "取消预约"
            };
        }

        public override void Approve(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Approved, "审批通过");
        }

        public override void Reject(VenueEventStateContext context, string reason)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Rejected, $"审批驳回: {reason}");
        }

        public override void Cancel(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Cancelled, "用户取消预约");
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 等待审批");
        }
    }
}

