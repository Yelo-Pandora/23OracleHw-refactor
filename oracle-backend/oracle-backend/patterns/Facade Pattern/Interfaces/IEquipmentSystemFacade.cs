// 外观模式：设备系统接口
// IEquipmentSystemFacade.cs

using oracle_backend.Patterns.State.Equipment;
using static oracle_backend.Controllers.EquipmentController;

namespace oracle_backend.patterns.Facade_Pattern.Interfaces
{
    /// <summary>
    /// 设备管理系统外观接口
    /// 封装了基于状态模式的设备操作、工单创建与维修确认流程
    /// </summary>
    public interface IEquipmentSystemFacade
    {
        /// <summary>
        /// 获取设备当前允许的操作列表 (State Context Query)
        /// </summary>
        Task<List<string>> GetAvailableActionsAsync(int equipmentId, string operatorId);

        /// <summary>
        /// 执行设备操作 (封装状态模式的状态转换)
        /// </summary>
        /// <param name="dto">操作参数</param>
        /// <returns>包含状态变更结果的对象</returns>
        Task<EquipmentOperationResult> OperateEquipmentAsync(EquipmentOperationDto dto);

        /// <summary>
        /// 创建维修工单 (State: Faulted -> UnderMaintenance)
        /// </summary>
        Task<(bool Success, string Message, object Data)> CreateRepairOrderAsync(CreateOrderDto dto);

        /// <summary>
        /// 确认维修完成 (State: UnderMaintenance -> Running/Faulted)
        /// </summary>
        Task<(bool Success, string Message)> ConfirmRepairAsync(OrderKeyDto dto);
    }
}
