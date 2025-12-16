// 废弃状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.Equipment
{
    /// <summary>
    /// 废弃状态 - 设备已废弃
    /// </summary>
    public class DiscardedState : EquipmentStateBase
    {
        public override string StateName => EquipmentStateContext.StateNames.Discarded;

        public override bool CanTransitionTo(string targetState)
        {
            // 废弃状态是终态,不能转换到其他状态
            return false;
        }

        public override List<string> GetAllowedOperations()
        {
            // 废弃状态下不允许任何操作
            return new List<string>();
        }

        protected override EquipmentOperationResult ProcessOperation(EquipmentStateContext context, string operation, string equipmentType)
        {
            return EquipmentOperationResult.CreateFailure("设备已被弃用,无法操作");
        }

        public override void OnEnter(EquipmentStateContext context)
        {
            Console.WriteLine($"[设备 {context.EquipmentId}] 已废弃");
        }
    }
}

