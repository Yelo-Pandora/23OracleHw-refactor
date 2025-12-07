// 维修中状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 维修中状态 - 设备正在维修
    /// </summary>
    public class UnderMaintenanceState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.UnderMaintenance;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == EquipmentStateContext.StateNames.Running => true,   // 维修成功后运行
                var s when s == EquipmentStateContext.StateNames.Faulted => true,    // 维修失败仍故障
                var s when s == EquipmentStateContext.StateNames.Discarded => true,  // 无法维修则废弃
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            // 维修中状态下不允许任何操作
            return new List<string>();
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            return EquipmentOperationResult.CreateFailure("设备当前处于维修中,不可操作");
        }

        public override void CompleteRepair(EquipmentStateContext context, bool success)
        {
            if (success)
            {
                context.TransitionToState(EquipmentStateContext.StateNames.Running, "维修成功");
            }
            else
            {
                context.TransitionToState(EquipmentStateContext.StateNames.Faulted, "维修失败");
            }
        }

        public override void OnEnter(EquipmentStateContext context)
        {
            Console.WriteLine($"[设备 {context.EquipmentId}] 进入维修状态");
        }
    }
}

