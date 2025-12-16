// 店铺状态接口
// Refactored with State Pattern

using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.Store
{
    /// <summary>
    /// 店铺状态接口
    /// </summary>
    public interface IStoreState : IState<StoreStateContext>
    {
        /// <summary>
        /// 判断是否可以提交状态变更申请
        /// </summary>
        /// <param name="context">店铺上下文</param>
        /// <param name="targetStatus">目标状态</param>
        /// <returns>是否可以申请</returns>
        bool CanRequestStatusChange(StoreStateContext context, string targetStatus);

        /// <summary>
        /// 处理状态变更申请
        /// </summary>
        /// <param name="context">店铺上下文</param>
        /// <param name="targetStatus">目标状态</param>
        /// <param name="reason">申请原因</param>
        /// <returns>申请结果</returns>
        StoreStatusChangeResult RequestStatusChange(StoreStateContext context, string targetStatus, string reason);

        /// <summary>
        /// 审批状态变更
        /// </summary>
        /// <param name="context">店铺上下文</param>
        /// <param name="approved">是否批准</param>
        /// <param name="targetStatus">目标状态</param>
        void ApproveStatusChange(StoreStateContext context, bool approved, string targetStatus);
    }

    /// <summary>
    /// 店铺状态变更结果
    /// </summary>
    public class StoreStatusChangeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ApplicationNo { get; set; }
        public bool RequiresApproval { get; set; }

        public static StoreStatusChangeResult CreateSuccess(string message, string applicationNo, bool requiresApproval)
        {
            return new StoreStatusChangeResult
            {
                Success = true,
                Message = message,
                ApplicationNo = applicationNo,
                RequiresApproval = requiresApproval
            };
        }

        public static StoreStatusChangeResult CreateFailure(string message)
        {
            return new StoreStatusChangeResult
            {
                Success = false,
                Message = message,
                RequiresApproval = false
            };
        }
    }
}

