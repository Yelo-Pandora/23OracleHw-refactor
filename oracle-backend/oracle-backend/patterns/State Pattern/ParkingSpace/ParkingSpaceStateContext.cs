// 车位状态上下文
// Refactored with State Pattern

using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.ParkingSpace
{
    /// <summary>
    /// 车位状态上下文,管理车位的占用状态转换
    /// </summary>
    public class ParkingSpaceStateContext : StateContextBase<IParkingSpaceState>
    {
        public int SpaceId { get; private set; }
        public string CurrentVehicle { get; private set; }

        // 状态常量
        public static class StateNames
        {
            public const string Empty = "空闲";
            public const string Occupied = "占用";
        }

        public ParkingSpaceStateContext(int spaceId, bool isOccupied, ILogger logger)
            : base(logger)
        {
            SpaceId = spaceId;

            // 注册所有可能的状态
            RegisterState(StateNames.Empty, new EmptyState());
            RegisterState(StateNames.Occupied, new OccupiedState());

            // 设置初始状态
            string initialStatus = isOccupied ? StateNames.Occupied : StateNames.Empty;
            _currentState = GetState(initialStatus);
            _currentState.OnEnter(this);
        }

        public override string CurrentStateName => _currentState.StateName;

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        public void TransitionToState(string targetStateName, string reason = "")
        {
            if (!_currentState.CanTransitionTo(targetStateName))
            {
                throw new InvalidOperationException($"不能从 {CurrentStateName} 转换到 {targetStateName}");
            }

            var oldState = _currentState;
            oldState.OnExit(this);

            var newState = GetState(targetStateName);
            TransitionTo(newState, reason);
            newState.OnEnter(this);
        }

        /// <summary>
        /// 判断是否可以执行指定操作
        /// </summary>
        public override bool CanPerformOperation(string operation)
        {
            return _currentState.GetAllowedOperations().Contains(operation);
        }

        /// <summary>
        /// 车辆进入
        /// </summary>
        public void EnterVehicle(string licensePlate)
        {
            _currentState.EnterVehicle(this, licensePlate);
            CurrentVehicle = licensePlate;
        }

        /// <summary>
        /// 车辆离开
        /// </summary>
        public void ExitVehicle()
        {
            _currentState.ExitVehicle(this);
            CurrentVehicle = null;
        }

        /// <summary>
        /// 判断车位是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return _currentState.IsAvailable(this);
        }

        /// <summary>
        /// 设置当前车辆
        /// </summary>
        public void SetCurrentVehicle(string licensePlate)
        {
            CurrentVehicle = licensePlate;
        }

        /// <summary>
        /// 清除当前车辆
        /// </summary>
        public void ClearCurrentVehicle()
        {
            CurrentVehicle = null;
        }
    }
}

