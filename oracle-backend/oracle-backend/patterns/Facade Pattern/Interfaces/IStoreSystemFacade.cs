// 外观模式：商店系统接口
// IStoreSystemFacade.cs

using static oracle_backend.Controllers.AreasController;
using static oracle_backend.Controllers.StoreController;

namespace oracle_backend.patterns.Facade_Pattern.Interfaces
{
    /// <summary>
    /// 商铺与区域管理系统外观接口
    /// 封装了基于状态模式的店铺营业状态变更及区域租赁状态变更
    /// </summary>
    public interface IStoreSystemFacade
    {
        /// <summary>
        /// 直接更新店铺状态 (State Context Transition)
        /// </summary>
        Task UpdateStoreStatusAsync(int storeId, string newStatus, string operatorAccount);

        /// <summary>
        /// 提交店铺状态变更申请 (State: CanRequestStatusChange)
        /// </summary>
        Task<(bool Success, string Message, string ApplicationNo)> SubmitStatusChangeRequestAsync(StoreStatusChangeRequestDto dto);

        /// <summary>
        /// 审批店铺状态变更 (State Transition via Approval)
        /// </summary>
        Task<(bool Success, string Message)> ApproveStatusChangeAsync(StoreStatusApprovalDto dto);

        /// <summary>
        /// 更新店面区域信息 (包含租赁状态校验)
        /// </summary>
        Task UpdateAreaInfoAsync(int areaId, AreaUpdateDto dto);
    }
}