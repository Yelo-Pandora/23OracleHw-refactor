// 场地活动状态基类
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.VenueEvent
{
    /// <summary>
    /// 场地活动状态基类,提供默认实现
    /// </summary>
    public abstract class VenueEventStateBase : IVenueEventState
    {
        public abstract string StateName { get; }

        public virtual void OnEnter(VenueEventStateContext context)
        {
            // 默认不执行任何操作
        }

        public virtual void OnExit(VenueEventStateContext context)
        {
            // 默认不执行任何操作
        }

        public abstract bool CanTransitionTo(string targetState);

        public abstract List<string> GetAllowedOperations();

        public virtual void Approve(VenueEventStateContext context)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持审批通过操作");
        }

        public virtual void Reject(VenueEventStateContext context, string reason)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持审批驳回操作");
        }

        public virtual void Start(VenueEventStateContext context)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持开始活动操作");
        }

        public virtual void End(VenueEventStateContext context)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持结束活动操作");
        }

        public virtual void Cancel(VenueEventStateContext context)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持取消活动操作");
        }

        public virtual bool CanModify(VenueEventStateContext context)
        {
            // 默认情况下,大部分状态不允许修改
            return false;
        }

        public virtual bool CanSettle(VenueEventStateContext context)
        {
            // 默认情况下,只有已结束状态可以结算
            return false;
        }
    }
}

