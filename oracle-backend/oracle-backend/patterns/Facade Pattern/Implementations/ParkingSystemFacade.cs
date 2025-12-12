// 外观模式：停车场系统具体实现类
// ParkingSystemFacade.cs

using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.patterns.Chain_of_Responsibility;
using oracle_backend.patterns.Facade_Pattern.Interfaces;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.patterns.Facade_Pattern.Implementations
{
    public class ParkingSystemFacade : IParkingSystemFacade
    {
        private readonly ParkingContext _parkingContext;
        private readonly IParkingRepository _parkingRepo;
        private readonly IAccountRepository _accountRepo;

        public ParkingSystemFacade(
            ParkingContext parkingContext,
            IParkingRepository parkingRepo,
            IAccountRepository accountRepo)
        {
            _parkingContext = parkingContext;
            _parkingRepo = parkingRepo;
            _accountRepo = accountRepo;
        }

        public async Task<(bool Success, string Message, object Data)> VehicleEntryAsync(string licensePlate, int spaceId, string operatorAccount)
        {
            // 权限校验：管理员
            var hasPermission = await _accountRepo.CheckAuthority(operatorAccount, 1);
            if (!hasPermission)
            {
                return (false, "权限不足，需要管理员权限", null);
            }

            // 调用责任链模式封装的入口
            var (success, errorCode, message) = await _parkingRepo.VehicleEntryAsync(licensePlate, spaceId);
            if (!success)
            {
                return (false, message, new { ErrorCode = errorCode });
            }

            // 读取刚刚写入的起始时间
            var record = await _parkingContext.PARK
                .Where(p => p.LICENSE_PLATE_NUMBER == licensePlate)
                .OrderByDescending(p => p.PARK_START)
                .FirstOrDefaultAsync();

            return (true, "车辆入场成功", new
            {
                LicensePlateNumber = licensePlate,
                ParkingSpaceId = spaceId,
                ParkStart = record?.PARK_START ?? DateTime.Now
            });
        }

        public async Task<(bool Success, string Message, object Data)> VehicleExitAsync(string licensePlate, string operatorAccount)
        {
            var hasPermission = await _accountRepo.CheckAuthority(operatorAccount, 1);
            if (!hasPermission)
            {
                return (false, "权限不足，需要管理员权限", null);
            }

            var result = await _parkingRepo.VehicleExitAsync(licensePlate);
            if (result == null)
            {
                return (false, "未找到车辆当日停车记录", null);
            }

            return (true, "已计算停车费用", result);
        }
    }
}