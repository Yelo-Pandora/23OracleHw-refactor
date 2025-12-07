// 商铺区域状态上下文
// Refactored with State Pattern

using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.RetailArea
{
    /// <summary>
    /// 商铺区域状态上下文,管理区域的租赁状态转换
    /// </summary>
    public class RetailAreaStateContext : StateContextBase<IRetailAreaState>
    {
        public int AreaId { get; private set; }
        public double BaseRent { get; private set; }
        public string CurrentTenant { get; private set; }

        // 状态常量
        public static class StateNames
        {
            public const string Vacant = "空置";
            public const string Rented = "已租用";
        }

        public RetailAreaStateContext(int areaId, double baseRent, string initialStatus, ILogger logger)
            : base(logger)
        {
            AreaId = areaId;
            BaseRent = baseRent;

            // 注册所有可能的状态
            RegisterState(StateNames.Vacant, new VacantState());
            RegisterState(StateNames.Rented, new RentedState());

            // 设置初始状态
            _currentState = GetState(initialStatus);
            _currentState.OnEnter(this);
        }

        public override string CurrentStateName => _currentState.StateName;

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        public void TransitionToState(string targetStateName, string reason = "")
        {
            if (!_currentState.CanTransitionTo(targetStateName))
            {
                throw new InvalidOperationException($"不能从 {CurrentStateName} 转换到 {targetStateName}");
            }

            var oldState = _currentState;
            oldState.OnExit(this);

            var newState = GetState(targetStateName);
            TransitionTo(newState, reason);
            newState.OnEnter(this);
        }

        /// <summary>
        /// 判断是否可以执行指定操作
        /// </summary>
        public override bool CanPerformOperation(string operation)
        {
            return _currentState.GetAllowedOperations().Contains(operation);
        }

        /// <summary>
        /// 判断是否可以出租
        /// </summary>
        public bool CanRent()
        {
            return _currentState.CanRent(this);
        }

        /// <summary>
        /// 租赁区域
        /// </summary>
        public void Rent(string tenantInfo)
        {
            _currentState.Rent(this, tenantInfo);
            CurrentTenant = tenantInfo;
        }

        /// <summary>
        /// 释放区域
        /// </summary>
        public void Release()
        {
            _currentState.Release(this);
            CurrentTenant = null;
        }

        /// <summary>
        /// 设置当前租户
        /// </summary>
        public void SetCurrentTenant(string tenant)
        {
            CurrentTenant = tenant;
        }
    }
}

