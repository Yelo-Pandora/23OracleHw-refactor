// 车位状态接口
// Refactored with State Pattern

using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.ParkingSpace
{
    /// <summary>
    /// 车位状态接口
    /// </summary>
    public interface IParkingSpaceState : IState<ParkingSpaceStateContext>
    {
        /// <summary>
        /// 车辆进入
        /// </summary>
        /// <param name="context">车位上下文</param>
        /// <param name="licensePlate">车牌号</param>
        void EnterVehicle(ParkingSpaceStateContext context, string licensePlate);

        /// <summary>
        /// 车辆离开
        /// </summary>
        /// <param name="context">车位上下文</param>
        void ExitVehicle(ParkingSpaceStateContext context);

        /// <summary>
        /// 判断是否可用
        /// </summary>
        /// <param name="context">车位上下文</param>
        /// <returns>是否可用</returns>
        bool IsAvailable(ParkingSpaceStateContext context);
    }
}

