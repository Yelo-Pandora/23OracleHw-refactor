// 已租用状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.RetailArea
{
    /// <summary>
    /// 已租用状态 - 区域已被租用
    /// </summary>
    public class RentedState : IRetailAreaState
    {
        public string StateName => RetailAreaStateContext.StateNames.Rented;

        public void OnEnter(RetailAreaStateContext context)
        {
            Console.WriteLine($"[区域 {context.AreaId}] 已被租用,租户: {context.CurrentTenant}");
        }

        public void OnExit(RetailAreaStateContext context)
        {
            Console.WriteLine($"[区域 {context.AreaId}] 租约结束");
        }

        public bool CanTransitionTo(string targetState)
        {
            return targetState == RetailAreaStateContext.StateNames.Vacant;
        }

        public List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "解除租约",
                "查看区域信息",
                "查看租户信息"
            };
        }

        public bool CanRent(RetailAreaStateContext context)
        {
            return false;
        }

        public void Rent(RetailAreaStateContext context, string tenantInfo)
        {
            throw new InvalidOperationException("区域已被租用,无法再次出租");
        }

        public void Release(RetailAreaStateContext context)
        {
            context.TransitionToState(RetailAreaStateContext.StateNames.Vacant, "租约结束或解除");
        }
    }
}

