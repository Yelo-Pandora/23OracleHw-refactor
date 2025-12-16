// 店铺状态上下文
// Refactored with State Pattern

using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 店铺状态上下文,管理店铺的状态转换
    /// </summary>
    public class StoreStateContext : StateContextBase<IStoreState>
    {
        public int StoreId { get; private set; }

        // 状态常量
        public static class StateNames
        {
            public const string NormalOperation = "正常营业";
            public const string Closed = "歇业中";
            public const string UnderRenovation = "翻新中";
        }

        public StoreStateContext(int storeId, string initialStatus, ILogger logger)
            : base(logger)
        {
            StoreId = storeId;

            // 注册所有可能的状态
            RegisterState(StateNames.NormalOperation, new NormalOperationState());
            RegisterState(StateNames.Closed, new ClosedState());
            RegisterState(StateNames.UnderRenovation, new UnderRenovationState());

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
        /// 请求状态变更
        /// </summary>
        public StoreStatusChangeResult RequestStatusChange(string targetStatus, string reason)
        {
            return _currentState.RequestStatusChange(this, targetStatus, reason);
        }

        /// <summary>
        /// 审批状态变更
        /// </summary>
        public void ApproveStatusChange(bool approved, string targetStatus)
        {
            _currentState.ApproveStatusChange(this, approved, targetStatus);
        }

        /// <summary>
        /// 判断是否可以提交状态变更申请
        /// </summary>
        public bool CanRequestStatusChange(string targetStatus)
        {
            return _currentState.CanRequestStatusChange(this, targetStatus);
        }
    }
}

