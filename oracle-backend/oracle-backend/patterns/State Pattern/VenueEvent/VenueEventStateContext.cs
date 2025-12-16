// 场地活动状态上下文
// Refactored with State Pattern

using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 场地活动状态上下文,管理活动的状态转换
    /// </summary>
    public class VenueEventStateContext : StateContextBase<IVenueEventState>
    {
        public int EventId { get; private set; }

        // 状态常量
        public static class StateNames
        {
            public const string PendingApproval = "待审批";
            public const string Approved = "已通过";
            public const string Rejected = "已驳回";
            public const string InProgress = "进行中";
            public const string Ended = "已结束";
            public const string Cancelled = "已取消";
        }

        public VenueEventStateContext(int eventId, string initialStatus, ILogger logger)
            : base(logger)
        {
            EventId = eventId;

            // 注册所有可能的状态
            RegisterState(StateNames.PendingApproval, new PendingApprovalState());
            RegisterState(StateNames.Approved, new ApprovedState());
            RegisterState(StateNames.Rejected, new RejectedState());
            RegisterState(StateNames.InProgress, new InProgressState());
            RegisterState(StateNames.Ended, new EndedState());
            RegisterState(StateNames.Cancelled, new CancelledState());

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
        /// 审批通过
        /// </summary>
        public void Approve()
        {
            _currentState.Approve(this);
        }

        /// <summary>
        /// 审批驳回
        /// </summary>
        public void Reject(string reason)
        {
            _currentState.Reject(this, reason);
        }

        /// <summary>
        /// 开始活动
        /// </summary>
        public void Start()
        {
            _currentState.Start(this);
        }

        /// <summary>
        /// 结束活动
        /// </summary>
        public void End()
        {
            _currentState.End(this);
        }

        /// <summary>
        /// 取消活动
        /// </summary>
        public void Cancel()
        {
            _currentState.Cancel(this);
        }

        /// <summary>
        /// 判断是否可以修改
        /// </summary>
        public bool CanModify()
        {
            return _currentState.CanModify(this);
        }

        /// <summary>
        /// 判断是否可以结算
        /// </summary>
        public bool CanSettle()
        {
            return _currentState.CanSettle(this);
        }
    }
}

