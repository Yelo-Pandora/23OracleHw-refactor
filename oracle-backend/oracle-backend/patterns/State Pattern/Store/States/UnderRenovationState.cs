// 翻新状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 翻新状态 - 店铺正在翻新
    /// </summary>
    public class UnderRenovationState : StoreStateBase
    {
        public override string StateName => StoreStateContext.StateNames.UnderRenovation;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == StoreStateContext.StateNames.NormalOperation => true,  // 翻新完成后恢复营业
                var s when s == StoreStateContext.StateNames.Closed => true,           // 可以转为歇业
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "更新翻新进度",
                "申请完成翻新",
                "查看历史记录"
            };
        }

        public override void OnEnter(StoreStateContext context)
        {
            Console.WriteLine($"[店铺 {context.StoreId}] 开始翻新");
        }

        public override void OnExit(StoreStateContext context)
        {
            Console.WriteLine($"[店铺 {context.StoreId}] 翻新结束");
        }
    }
}

