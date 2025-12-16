// 已驳回状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 已驳回状态 - 活动预约被驳回
    /// </summary>
    public class RejectedState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.Rejected;

        public override bool CanTransitionTo(string targetState)
        {
            // 被驳回后一般不能再转换到其他状态
            return false;
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "查看驳回原因"
            };
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 已被驳回");
        }
    }
}

