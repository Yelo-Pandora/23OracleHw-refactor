// 设备状态接口
// Refactored with State Pattern

using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 设备状态接口
    /// </summary>
    public interface IEquipmentState : IState<EquipmentStateContext>
    {
        /// <summary>
        /// 处理设备操作请求
        /// </summary>
        /// <param name="context">设备上下文</param>
        /// <param name="operation">操作名称</param>
        /// <param name="equipmentType">设备类型</param>
        /// <returns>操作结果</returns>
        EquipmentOperationResult HandleOperation(EquipmentStateContext context, string operation, string equipmentType);

        /// <summary>
        /// 创建维修工单
        /// </summary>
        /// <param name="context">设备上下文</param>
        /// <returns>是否可以创建工单</returns>
        bool CanCreateRepairOrder(EquipmentStateContext context);

        /// <summary>
        /// 完成维修
        /// </summary>
        /// <param name="context">设备上下文</param>
        /// <param name="success">维修是否成功</param>
        void CompleteRepair(EquipmentStateContext context, bool success);
    }

    /// <summary>
    /// 设备操作结果
    /// </summary>
    public class EquipmentOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NewStatus { get; set; }
        public bool StatusChanged { get; set; }

        public static EquipmentOperationResult CreateSuccess(string message, string newStatus, bool statusChanged)
        {
            return new EquipmentOperationResult
            {
                Success = true,
                Message = message,
                NewStatus = newStatus,
                StatusChanged = statusChanged
            };
        }

        public static EquipmentOperationResult CreateFailure(string message)
        {
            return new EquipmentOperationResult
            {
                Success = false,
                Message = message,
                StatusChanged = false
            };
        }
    }
}

