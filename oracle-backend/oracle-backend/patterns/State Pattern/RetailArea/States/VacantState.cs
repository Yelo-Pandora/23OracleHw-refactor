// 空置状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.RetailArea
{
    /// <summary>
    /// 空置状态 - 区域未被租用
    /// </summary>
    public class VacantState : IRetailAreaState
    {
        public string StateName => RetailAreaStateContext.StateNames.Vacant;

        public void OnEnter(RetailAreaStateContext context)
        {
            Console.WriteLine($"[区域 {context.AreaId}] 进入空置状态,可供出租");
        }

        public void OnExit(RetailAreaStateContext context)
        {
            Console.WriteLine($"[区域 {context.AreaId}] 离开空置状态");
        }

        public bool CanTransitionTo(string targetState)
        {
            return targetState == RetailAreaStateContext.StateNames.Rented;
        }

        public List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "出租",
                "查看区域信息",
                "更新区域信息"
            };
        }

        public bool CanRent(RetailAreaStateContext context)
        {
            return true;
        }

        public void Rent(RetailAreaStateContext context, string tenantInfo)
        {
            context.TransitionToState(RetailAreaStateContext.StateNames.Rented, $"租给 {tenantInfo}");
        }

        public void Release(RetailAreaStateContext context)
        {
            throw new InvalidOperationException("区域已经是空置状态,无法释放");
        }
    }
}

