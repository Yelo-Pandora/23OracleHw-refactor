// 设备状态基类
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 设备状态基类,提供默认实现
    /// </summary>
    public abstract class EquipmentStateBase : IEquipmentState
    {
        public abstract string StateName { get; }

        public virtual void OnEnter(EquipmentStateContext context)
        {
            // 默认不执行任何操作
        }

        public virtual void OnExit(EquipmentStateContext context)
        {
            // 默认不执行任何操作
        }

        public abstract bool CanTransitionTo(string targetState);

        public abstract List<string> GetAllowedOperations();

        public virtual EquipmentOperationResult HandleOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            var allowedOps = GetAllowedOperations();
            if (!allowedOps.Contains(operation))
            {
                return EquipmentOperationResult.CreateFailure($"当前状态 {StateName} 不支持操作: {operation}");
            }

            // 处理紧急停止(所有非废弃状态都支持)
            if (operation == "紧急停止" && StateName != EquipmentStateContext.StateNames.Discarded)
            {
                context.TransitionToState(EquipmentStateContext.StateNames.Faulted, "紧急停止");
                return EquipmentOperationResult.CreateSuccess(
                    "紧急制停操作成功,设备已进入故障状态",
                    EquipmentStateContext.StateNames.Faulted,
                    true
                );
            }

            return ProcessOperation(context, operation, equipmentType);
        }

        /// <summary>
        /// 处理具体操作,由子类实现
        /// </summary>
        protected abstract EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType);

        public virtual bool CanCreateRepairOrder(EquipmentStateContext context)
        {
            // 默认情况下,只有故障状态可以创建维修工单
            return false;
        }

        public virtual void CompleteRepair(EquipmentStateContext context, bool success)
        {
            throw new InvalidOperationException($"状态 {StateName} 不支持完成维修操作");
        }
    }
}

