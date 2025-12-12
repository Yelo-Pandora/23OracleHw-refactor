// 外观模式：停车场系统接口
// IParkingSystemFacade.cs

namespace oracle_backend.patterns.Facade_Pattern.Interfaces
{
    /// <summary>
    /// 停车场系统外观接口
    /// 封装了基于责任链模式的车辆入场校验流程，以及出场支付流程
    /// </summary>
    public interface IParkingSystemFacade
    {
        /// <summary>
        /// 车辆入场处理 (封装责任链模式)
        /// </summary>
        /// <param name="licensePlate">车牌号</param>
        /// <param name="spaceId">车位ID</param>
        /// <param name="operatorAccount">操作员账号</param>
        /// <returns>包含成功状态和消息的结果对象</returns>
        Task<(bool Success, string Message, object Data)> VehicleEntryAsync(string licensePlate, int spaceId, string operatorAccount);

        /// <summary>
        /// 车辆出场处理
        /// </summary>
        Task<(bool Success, string Message, object Data)> VehicleExitAsync(string licensePlate, string operatorAccount);
    }
}
