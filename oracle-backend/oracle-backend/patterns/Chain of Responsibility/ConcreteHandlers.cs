using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;

namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 1. 车位存在性校验处理者
    /// 检查车位是否存在
    /// 优化：使用异常处理，代码更简洁
    /// </summary>
    public class SpaceExistenceHandler : EntryHandler
    {
        private readonly ParkingContext _context;

        public SpaceExistenceHandler(ParkingContext context)
        {
            _context = context;
        }

        public override async Task<bool> HandleAsync(VehicleEntryRequest request)
        {
            // 检查车位是否存在
            var space = await _context.PARKING_SPACE
                .FirstOrDefaultAsync(ps => ps.PARKING_SPACE_ID == request.SpaceId);

            if (space == null)
            {
                throw new VehicleEntryException(VehicleEntryErrorCode.ParkingSpaceNotFound, $"车位 {request.SpaceId} 不存在");
            }

            // 将车位信息存储到请求对象中，供后续处理者使用
            request.ParkingSpace = space;

            // 校验通过，传递给下一个处理者
            return await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 2. 重复车辆校验处理者
    /// 检查车辆是否已在停车场内（未出场）
    /// 优化：简化查询逻辑，使用异常处理
    /// </summary>
    public class DuplicateVehicleHandler : EntryHandler
    {
        private readonly ParkingContext _context;

        public DuplicateVehicleHandler(ParkingContext context)
        {
            _context = context;
        }

        public override async Task<bool> HandleAsync(VehicleEntryRequest request)
        {
            // 检查车辆是否还在停车场内（只有当前还在停车的车辆才不能重复入场）
            var activeCars = await _context.CAR
                .Where(c => c.LICENSE_PLATE_NUMBER == request.LicensePlate && !c.PARK_END.HasValue)
                .AnyAsync();

            if (activeCars)
            {
                throw new VehicleEntryException(VehicleEntryErrorCode.VehicleAlreadyInside, "该车辆当前还在停车场内，不可重复入场");
            }

            // 校验通过，传递给下一个处理者
            return await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 3. 车位状态校验处理者
    /// 检查车位是否已被占用
    /// 优化：简化查询逻辑，使用异常处理
    /// </summary>
    public class SpaceStatusHandler : EntryHandler
    {
        private readonly ParkingContext _context;

        public SpaceStatusHandler(ParkingContext context)
        {
            _context = context;
        }

        public override async Task<bool> HandleAsync(VehicleEntryRequest request)
        {
            // 直接检查车位的OCCUPIED字段，性能更优
            var space = await _context.PARKING_SPACE
                .FirstOrDefaultAsync(ps => ps.PARKING_SPACE_ID == request.SpaceId);

            if (space != null && space.OCCUPIED)
            {
                // 当车位被占用时，获取占用车辆信息（仅用于错误提示）
                var occupyingVehicle = await _context.PARK
                    .Where(p => p.PARKING_SPACE_ID == request.SpaceId)
                    .Join(_context.CAR,
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          c => new { LicensePlate = c.LICENSE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          (p, c) => new { Park = p, Car = c })
                    .Where(pc => !pc.Car.PARK_END.HasValue)
                    .Select(pc => pc.Park.LICENSE_PLATE_NUMBER)
                    .FirstOrDefaultAsync();

                if (occupyingVehicle != null)
                {
                    throw new VehicleEntryException(VehicleEntryErrorCode.ParkingSpaceOccupied, $"车位 {request.SpaceId} 已被车辆 {occupyingVehicle} 占用");
                }
                else
                {
                    throw new VehicleEntryException(VehicleEntryErrorCode.ParkingSpaceOccupied, $"车位 {request.SpaceId} 已被占用");
                }
            }

            // 校验通过，传递给下一个处理者
            return await base.HandleAsync(request);
        }
    }





    /// <summary>
    /// 6. 最终处理者 - 执行实际的入场操作
    /// 所有校验通过后，执行车辆入场操作
    /// 优化：使用异常处理，简化返回值
    /// </summary>
    public class VehicleEntryExecutor : EntryHandler
    {
        private readonly ParkingContext _context;

        public VehicleEntryExecutor(ParkingContext context)
        {
            _context = context;
        }

        public override async Task<bool> HandleAsync(VehicleEntryRequest request)
        {
            // 使用中国本地时间戳
            var currentTime = DateTime.Now;

            // 1. 插入车辆记录
            var car = new Car
            {
                LICENSE_PLATE_NUMBER = request.LicensePlate,
                PARK_START = currentTime,
                PARK_END = null
            };
            _context.CAR.Add(car);

            // 2. 插入停车记录
            var park = new Park
            {
                LICENSE_PLATE_NUMBER = request.LicensePlate,
                PARKING_SPACE_ID = request.SpaceId,
                PARK_START = currentTime
            };
            _context.PARK.Add(park);

            // 3. 保存到数据库
            await _context.SaveChangesAsync();

            // 4. 更新车位状态（使用原生SQL）
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE PARKING_SPACE SET OCCUPIED = 1 WHERE PARKING_SPACE_ID = {0}",
                request.SpaceId);

            // 5. 记录日志
            Console.WriteLine($"[停车记录创建成功] 车牌号: {request.LicensePlate}, 车位ID: {request.SpaceId}");
            Console.WriteLine($"  入场时间: {currentTime:yyyy-MM-dd HH:mm:ss} (中国本地时间)");
            Console.WriteLine($"  时区信息: {TimeZoneInfo.Local.DisplayName}");

            return true;
        }
    }
}



