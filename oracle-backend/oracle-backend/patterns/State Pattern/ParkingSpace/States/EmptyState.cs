// 空闲状态
// Refactored with State Pattern

namespace oracle_backend.Patterns.State.ParkingSpace
{
    /// <summary>
    /// 空闲状态 - 车位空闲,可以停车
    /// </summary>
    public class EmptyState : IParkingSpaceState
    {
        public string StateName => ParkingSpaceStateContext.StateNames.Empty;

        public void OnEnter(ParkingSpaceStateContext context)
        {
            Console.WriteLine($"[车位 {context.SpaceId}] 空闲,可供停车");
        }

        public void OnExit(ParkingSpaceStateContext context)
        {
            Console.WriteLine($"[车位 {context.SpaceId}] 被占用");
        }

        public bool CanTransitionTo(string targetState)
        {
            return targetState == ParkingSpaceStateContext.StateNames.Occupied;
        }

        public List<string> GetAllowedOperations()
        {
            return new List<string>
            {
                "停车",
                "查看车位信息"
            };
        }

        public void EnterVehicle(ParkingSpaceStateContext context, string licensePlate)
        {
            context.TransitionToState(ParkingSpaceStateContext.StateNames.Occupied, $"车辆 {licensePlate} 进入");
        }

        public void ExitVehicle(ParkingSpaceStateContext context)
        {
            throw new InvalidOperationException("车位已经是空闲状态,没有车辆可以离开");
        }

        public bool IsAvailable(ParkingSpaceStateContext context)
        {
            return true;
        }
    }
}

