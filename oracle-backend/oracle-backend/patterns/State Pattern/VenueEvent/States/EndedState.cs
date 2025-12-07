// 已结束状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 已结束状态 - 活动已经结束
    /// </summary>
    public class EndedState : VenueEventStateBase
    {
        public override string StateName => VenueEventStateContext.StateNames.Ended;

        public override bool CanTransitionTo(string targetState)
        {
            // 已结束状态是终态,不能转换到其他状态
            return false;
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "结算费用",
                "查看活动信息",
                "生成报告"
            };
        }

        public override bool CanSettle(VenueEventStateContext context)
        {
            return true;
        }

        public override void OnEnter(VenueEventStateContext context)
        {
            Console.WriteLine($"[活动 {context.EventId}] 活动已结束,可以进行结算");
        }
    }
}

