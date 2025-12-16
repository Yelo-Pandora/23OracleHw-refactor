// 正常营业状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 正常营业状态 - 店铺正常营业中
    /// </summary>
    public class NormalOperationState : StoreStateBase
    {
        public override string StateName => StoreStateContext.StateNames.NormalOperation;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == StoreStateContext.StateNames.Closed => true,           // 可以歇业
                var s when s == StoreStateContext.StateNames.UnderRenovation => true,  // 可以翻新
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "申请歇业",
                "申请翻新",
                "更新商品信息",
                "处理订单",
                "管理库存"
            };
        }

        public override void OnEnter(StoreStateContext context)
        {
            Console.WriteLine($"[店铺 {context.StoreId}] 开始正常营业");
        }
    }
}

