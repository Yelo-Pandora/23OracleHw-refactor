// 离线状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 离线状态 - 设备离线
    /// </summary>
    public class OfflineState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.Offline;

        public override bool CanTransitionTo(string targetState)
        {
            return targetState switch
            {
                var s when s == EquipmentStateContext.StateNames.Standby => true,    // 可以上线到待机
                var s when s == EquipmentStateContext.StateNames.Running => true,    // 可以上线到运行
                var s when s == EquipmentStateContext.StateNames.Discarded => true,  // 可以废弃
                _ => false
            };
        }

        public override List<string> GetAllowedOperations()
        {
            // 离线状态下不允许任何操作
            return new List<string>();
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            return EquipmentOperationResult.CreateFailure("设备当前处于离线状态,不可操作");
        }

        public override void OnEnter(EquipmentStateContext context)
        {
            Console.WriteLine($"[设备 {context.EquipmentId}] 离线");
        }
    }
}

