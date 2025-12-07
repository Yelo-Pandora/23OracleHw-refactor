// 状态上下文基类 - 所有状态上下文的基础类
// Refactored with State Pattern

using Microsoft.Extensions.Logging;

namespace oracle_backend.Patterns.State.Core
{
    /// <summary>
    /// 状态上下文基类,管理状态的转换和当前状态
    /// </summary>
    /// <typeparam name="TState">状态接口类型</typeparam>
    public abstract class StateContextBase<TState> where TState : class
    {
        protected TState _currentState;
        protected readonly ILogger _logger;
        protected readonly Dictionary<string, TState> _stateRegistry = new();

        protected StateContextBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TState CurrentState => _currentState;

        /// <summary>
        /// 当前状态名称
        /// </summary>
        public abstract string CurrentStateName { get; }

        /// <summary>
        /// 注册状态
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <param name="state">状态实例</param>
        protected void RegisterState(string stateName, TState state)
        {
            _stateRegistry[stateName] = state;
        }

        /// <summary>
        /// 获取注册的状态
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <returns>状态实例</returns>
        protected TState GetState(string stateName)
        {
            if (_stateRegistry.TryGetValue(stateName, out var state))
            {
                return state;
            }
            throw new InvalidOperationException($"状态 {stateName} 未注册");
        }

        /// <summary>
        /// 转换到新状态
        /// </summary>
        /// <param name="newState">新状态实例</param>
        /// <param name="reason">转换原因(用于日志)</param>
        protected void TransitionTo(TState newState, string reason = "")
        {
            var oldStateName = CurrentStateName;
            _currentState = newState;
            
            var reasonLog = string.IsNullOrEmpty(reason) ? "" : $", 原因: {reason}";
            _logger.LogInformation($"状态转换: {oldStateName} -> {CurrentStateName}{reasonLog}");
        }

        /// <summary>
        /// 判断是否可以执行指定操作
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <returns>是否可以执行</returns>
        public abstract bool CanPerformOperation(string operation);
    }
}

