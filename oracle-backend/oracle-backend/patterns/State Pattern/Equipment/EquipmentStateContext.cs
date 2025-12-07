// 设备状态上下文
// Refactored with State Pattern

using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 设备状态上下文,管理设备的状态转换
    /// </summary>
    public class EquipmentStateContext : StateContextBase<IEquipmentState>
    {
        public int EquipmentId { get; private set; }
        public string EquipmentType { get; private set; }

        // 状态常量
        public static class StateNames
        {
            public const string Running = "运行中";
            public const string Faulted = "故障";
            public const string Offline = "离线";
            public const string UnderMaintenance = "维修中";
            public const string Standby = "待机";
            public const string Discarded = "废弃";
        }

        public EquipmentStateContext(int equipmentId, string equipmentType, string initialStatus, ILogger logger)
            : base(logger)
        {
            EquipmentId = equipmentId;
            EquipmentType = equipmentType;

            // 注册所有可能的状态
            RegisterState(StateNames.Running, new RunningState());
            RegisterState(StateNames.Faulted, new FaultedState());
            RegisterState(StateNames.Offline, new OfflineState());
            RegisterState(StateNames.UnderMaintenance, new UnderMaintenanceState());
            RegisterState(StateNames.Standby, new StandbyState());
            RegisterState(StateNames.Discarded, new DiscardedState());

            // 设置初始状态
            _currentState = GetState(initialStatus);
            _currentState.OnEnter(this);
        }

        public override string CurrentStateName => _currentState.StateName;

        /// <summary>
        /// 执行设备操作
        /// </summary>
        public EquipmentOperationResult PerformOperation(string operation, string equipmentType)
        {
            return _currentState.HandleOperation(this, operation, equipmentType);
        }

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
        /// 获取当前状态下允许的所有操作
        /// </summary>
        public List<string> GetAllowedOperations()
        {
            return _currentState.GetAllowedOperations();
        }

        /// <summary>
        /// 创建维修工单
        /// </summary>
        public bool CanCreateRepairOrder()
        {
            return _currentState.CanCreateRepairOrder(this);
        }

        /// <summary>
        /// 完成维修
        /// </summary>
        public void CompleteRepair(bool success)
        {
            _currentState.CompleteRepair(this, success);
        }
    }
}

