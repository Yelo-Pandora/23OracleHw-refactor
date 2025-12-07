// 占用状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.ParkingSpace
{
    /// <summary>
    /// 占用状态 - 车位被占用
    /// </summary>
    public class OccupiedState : IParkingSpaceState
    {
        public string StateName => ParkingSpaceStateContext.StateNames.Occupied;

        public void OnEnter(ParkingSpaceStateContext context)
        {
            Console.WriteLine($"[车位 {context.SpaceId}] 已被车辆占用: {context.CurrentVehicle}");
        }

        public void OnExit(ParkingSpaceStateContext context)
        {
            Console.WriteLine($"[车位 {context.SpaceId}] 车辆离开");
        }

        public bool CanTransitionTo(string targetState)
        {
            return targetState == ParkingSpaceStateContext.StateNames.Empty;
        }

        public List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "离场",
                "查看车位信息",
                "查看车辆信息"
            };
        }

        public void EnterVehicle(ParkingSpaceStateContext context, string licensePlate)
        {
            throw new InvalidOperationException("车位已被占用,无法再次停车");
        }

        public void ExitVehicle(ParkingSpaceStateContext context)
        {
            context.TransitionToState(ParkingSpaceStateContext.StateNames.Empty, $"车辆 {context.CurrentVehicle} 离开");
        }

        public bool IsAvailable(ParkingSpaceStateContext context)
        {
            return false;
        }
    }
}

