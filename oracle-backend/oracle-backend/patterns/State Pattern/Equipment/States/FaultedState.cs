// 故障状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 故障状态 - 设备发生故障
    /// </summary>
    public class FaultedState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.Faulted;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == EquipmentStateContext.StateNames.UnderMaintenance => true,  // 可以进入维修
                var s when s == EquipmentStateContext.StateNames.Discarded => true,         // 可以废弃
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            // 故障状态下不允许任何操作
            return new List<string>();
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            return EquipmentOperationResult.CreateFailure("设备当前处于故障状态,不可操作");
        }

        public override bool CanCreateRepairOrder(EquipmentStateContext context)
        {
            // 故障状态可以创建维修工单
            return true;
        }

        public override void OnEnter(EquipmentStateContext context)
        {
            // 进入故障状态时的日志
            Console.WriteLine($"[设备 {context.EquipmentId}] 进入故障状态,需要创建维修工单");
        }
    }
}

