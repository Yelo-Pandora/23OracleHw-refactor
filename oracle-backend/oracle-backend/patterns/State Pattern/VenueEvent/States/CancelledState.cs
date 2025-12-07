// 已取消状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 已取消状态 - 活动已取消
    /// </summary>
    public class CancelledState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.Cancelled;

        public override bool CanTransitionTo(string targetState)
        {
            // 已取消状态是终态,不能转换到其他状态
            return false;
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "查看活动信息"
            };
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 活动已取消");
        }
    }
}

