// 状态模式核心接口 - 所有状态的基础接口
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Core
{
    /// <summary>
    /// 状态基础接口,定义状态的基本行为
    /// </summary>
    /// <typeparam name="TContext">状态上下文类型</typeparam>
    public interface IState<TContext> where TContext : class
    {
        /// <summary>
        /// 获取状态名称
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// 进入该状态时的处理逻辑
        /// </summary>
        /// <param name="context">状态上下文</param>
        void OnEnter(TContext context);

        /// <summary>
        /// 退出该状态时的处理逻辑
        /// </summary>
        /// <param name="context">状态上下文</param>
        void OnExit(TContext context);

        /// <summary>
        /// 判断是否可以从当前状态转换到目标状态
        /// </summary>
        /// <param name="targetState">目标状态名称</param>
        /// <returns>是否可以转换</returns>
        bool CanTransitionTo(string targetState);

        /// <summary>
        /// 获取该状态下允许的所有操作
        /// </summary>
        /// <returns>操作列表</returns>
        List<string> GetAllowedOperations();
    }
}

