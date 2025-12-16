// 店铺状态基类
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 店铺状态基类,提供默认实现
    /// </summary>
    public abstract class StoreStateBase : IStoreState
    {
        public abstract string StateName { get; }

        public virtual void OnEnter(StoreStateContext context)
        {
            // 默认不执行任何操作
        }

        public virtual void OnExit(StoreStateContext context)
        {
            // 默认不执行任何操作
        }

        public abstract bool CanTransitionTo(string targetState);

        public abstract List<string> GetAllowedOperations();

        public virtual bool CanRequestStatusChange(StoreStateContext context, string targetStatus)
        {
            return CanTransitionTo(targetStatus);
        }

        public virtual StoreStatusChangeResult RequestStatusChange(StoreStateContext context, string targetStatus, string reason)
        {
            if (!CanRequestStatusChange(context, targetStatus))
            {
                return StoreStatusChangeResult.CreateFailure($"不能从 {StateName} 状态变更到 {targetStatus}");
            }

            // 生成申请单号
            var applicationNo = $"SA{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";

            return StoreStatusChangeResult.CreateSuccess(
                "状态变更申请已提交,等待审批",
                applicationNo,
                true
            );
        }

        public virtual void ApproveStatusChange(StoreStateContext context, bool approved, string targetStatus)
        {
            if (approved && CanTransitionTo(targetStatus))
            {
                context.TransitionToState(targetStatus, "审批通过");
            }
        }
    }
}

