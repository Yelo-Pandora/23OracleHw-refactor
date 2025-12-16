// 歇业状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 歇业状态 - 店铺暂时关闭
    /// </summary>
    public class ClosedState : StoreStateBase
    {
        public override string StateName => StoreStateContext.StateNames.Closed;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == StoreStateContext.StateNames.NormalOperation => true,  // 可以恢复营业
                var s when s == StoreStateContext.StateNames.UnderRenovation => true,  // 可以翻新
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "申请恢复营业",
                "申请翻新",
                "查看历史记录"
            };
        }

        public override void OnEnter(StoreStateContext context)
        {
            Console.WriteLine($"[店铺 {context.StoreId}] 进入歇业状态");
        }

        public override void OnExit(StoreStateContext context)
        {
            Console.WriteLine($"[店铺 {context.StoreId}] 结束歇业状态");
        }
    }
}

