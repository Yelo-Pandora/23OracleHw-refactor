// 待机状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 待机状态 - 设备待机中
    /// </summary>
    public class StandbyState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.Standby;

        private static readonly string[] StandbyAirActions = { "开机", "紧急停止" };
        private static readonly string[] StandbyLightActions = { "开灯", "紧急停止" };
        private static readonly string[] StandbyElevatorActions = { "启动", "紧急停止" };

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == EquipmentStateContext.StateNames.Running => true,    // 可以启动
                var s when s == EquipmentStateContext.StateNames.Faulted => true,     // 可能故障
                var s when s == EquipmentStateContext.StateNames.Offline => true,    // 可以离线
                var s when s == EquipmentStateContext.StateNames.Discarded => true,  // 可以废弃
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            return new List<string> { "紧急停止" };
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            operation = operation.ToLower();
            equipmentType = equipmentType.ToLower();

            // 检查操作是否有效
            string[] validOperations = equipmentType switch
            {
                "空调" => StandbyAirActions,
                "照明" => StandbyLightActions,
                "电梯" => StandbyElevatorActions,
                _ => Array.Empty<string>()
            };

            if (!validOperations.Any(op => op.ToLower() == operation))
            {
                return EquipmentOperationResult.CreateFailure($"设备类型 {equipmentType} 在待机状态下不支持操作 {operation}");
            }

            // 判断是否需要转换到运行状态
            bool shouldTransitionToRunning = (operation == "开机" && equipmentType == "空调") ||
                                             (operation == "开灯" && equipmentType == "照明") ||
                                             (operation == "启动" && equipmentType == "电梯");

            if (shouldTransitionToRunning)
            {
                context.TransitionToState(EquipmentStateContext.StateNames.Running, $"执行操作: {operation}");
                return EquipmentOperationResult.CreateSuccess(
                    "操作成功,设备进入运行状态",
                    EquipmentStateContext.StateNames.Running,
                    true
                );
            }

            return EquipmentOperationResult.CreateSuccess(
                "操作成功",
                EquipmentStateContext.StateNames.Standby,
                false
            );
        }
    }
}

