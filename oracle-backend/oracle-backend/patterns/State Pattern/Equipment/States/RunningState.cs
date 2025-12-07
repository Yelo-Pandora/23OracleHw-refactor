// 运行中状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 运行中状态 - 设备正常运行
    /// </summary>
    public class RunningState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.Running;

        private static readonly string[] AirConditionerActions = { "关机", "制冷模式", "制热模式", "调节温度", "紧急停止" };
        private static readonly string[] LightingActions = { "关灯", "调亮", "调暗", "紧急停止" };
        private static readonly string[] ElevatorActions = { "停止", "开门", "关门", "紧急停止" };

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == EquipmentStateContext.StateNames.Standby => true,    // 可以待机
                var s when s == EquipmentStateContext.StateNames.Faulted => true,     // 可能故障
                var s when s == EquipmentStateContext.StateNames.Offline => true,    // 可以离线
                var s when s == EquipmentStateContext.StateNames.Discarded => true,  // 可以废弃
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            // 运行中状态不允许开机/开灯/启动操作,因为已经在运行
            var operations = new List<string> { "紧急停止" };
            // 根据设备类型添加特定操作,在HandleOperation中动态处理
            return operations;
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            operation = operation.ToLower();
            equipmentType = equipmentType.ToLower();

            // 检查操作是否有效
            string[] validOperations = equipmentType switch
            {
                "空调" => AirConditionerActions,
                "照明" => LightingActions,
                "电梯" => ElevatorActions,
                _ => Array.Empty<string>()
            };

            if (!validOperations.Any(op => op.ToLower() == operation))
            {
                return EquipmentOperationResult.CreateFailure($"设备类型 {equipmentType} 在运行状态下不支持操作 {operation}");
            }

            // 判断是否需要转换到待机状态
            bool shouldTransitionToStandby = (operation == "关机" && equipmentType == "空调") ||
                                             (operation == "关灯" && equipmentType == "照明") ||
                                             (operation == "停止" && equipmentType == "电梯");

            if (shouldTransitionToStandby)
            {
                context.TransitionToState(EquipmentStateContext.StateNames.Standby, $"执行操作: {operation}");
                return EquipmentOperationResult.CreateSuccess(
                    "操作成功,设备进入待机状态",
                    EquipmentStateContext.StateNames.Standby,
                    true
                );
            }

            // 其他操作不改变状态
            return EquipmentOperationResult.CreateSuccess(
                "操作成功",
                EquipmentStateContext.StateNames.Running,
                false
            );
        }
    }
}

