// 进行中状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 进行中状态 - 活动正在进行
    /// </summary>
    public class InProgressState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.InProgress;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == VenueEventStateContext.StateNames.Ended => true,       // 可以结束
                var s when s == VenueEventStateContext.StateNames.Cancelled => true,   // 可以取消
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "结束活动",
                "取消活动",
                "查看活动信息"
            };
        }

        public override void End(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Ended, "活动正常结束");
        }

        public override void Cancel(VenueEventStateContext context)
        {
            context.TransitionToState(VenueEventStateContext.StateNames.Cancelled, "活动提前取消");
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 活动开始进行");
        }
    }
}

