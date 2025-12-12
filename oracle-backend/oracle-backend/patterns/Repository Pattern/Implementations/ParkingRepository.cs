using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using static oracle_backend.Controllers.ParkingController;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class ParkingRepository : BaseRepository<ParkingLot>, IParkingRepository
    {
        // 强类型引用 ParkingContext
        private readonly ParkingContext _parkingContext;

        public ParkingRepository(ParkingContext context) : base(context)
        {
            _parkingContext = context;
        }

        // 停车场信息管理
        public async Task<bool> ParkingLotExistsAsync(int areaId)
        {
            return await _parkingContext.ParkingLotExists(areaId);
        }

        public async Task<ParkingLot?> GetParkingLotByIdAsync(int areaId)
        {
            return await _parkingContext.GetParkingLotById(areaId);
        }

        public async Task<bool> UpdateParkingLotInfoAsync(int areaId, int parkingFee, string status, int? parkingSpaceCount = null)
        {
            return await _parkingContext.UpdateParkingLotInfo(areaId, parkingFee, status, parkingSpaceCount);
        }

        public Task<string> GetParkingLotStatusAsync(int areaId)
        {
            // Context 中是同步方法（读取内存字典），这里包装成 Async
            return Task.FromResult(_parkingContext.GetParkingLotStatus(areaId));
        }

        public async Task<int> GetParkingLotSpaceCountAsync(int areaId)
        {
            return await _parkingContext.GetParkingLotSpaceCount(areaId);
        }

        public async Task<List<dynamic>> GetParkingLotListAsync()
        {
            return await _parkingContext.GetParkingLotList();
        }

        // 车位管理
        public async Task<List<ParkingSpace>> GetParkingSpacesAsync(int? areaId = null)
        {
            return await _parkingContext.GetParkingSpaces(areaId);
        }

        public async Task<ParkingStatusStatistics> GetParkingStatusStatisticsAsync(int areaId)
        {
            return await _parkingContext.GetParkingStatusStatistics(areaId);
        }

        public async Task<List<ParkingSpaceStatus>> GetParkingSpaceStatusesAsync(int areaId)
        {
            return await _parkingContext.GetParkingSpaceStatuses(areaId);
        }

        public async Task<bool> IsParkingSpaceAvailableAsync(int parkingSpaceId)
        {
            return await _parkingContext.IsParkingSpaceAvailable(parkingSpaceId);
        }

        // 车辆管理 (核心业务逻辑)
        public async Task<(bool Success, int ErrorCode, string Message)> VehicleEntryAsync(string licensePlate, int spaceId)
        {
            return await _parkingContext.VehicleEntry(licensePlate, spaceId);
        }

        public async Task<VehicleExitResult?> VehicleExitAsync(string licensePlate)
        {
            return await _parkingContext.VehicleExit(licensePlate);
        }

        public async Task<List<VehicleStatusResult>> GetCurrentVehiclesAsync(int? areaId = null)
        {
            return await _parkingContext.GetCurrentVehicles(areaId);
        }

        // 责任链处理者所需方法实现
        public async Task<bool> IsVehicleBlacklistedAsync(string licensePlate)
        {
            // 检查车辆是否在黑名单中
            return await _parkingContext.BLACKLIST
                .AnyAsync(b => b.LICENSE_PLATE_NUMBER == licensePlate && b.STATUS == "ACTIVE");
        }

        public async Task<bool> HasSufficientBalanceAsync(string licensePlate)
        {
            // 检查车辆用户的余额是否充足（至少10元）
            return await _parkingContext.USER_BALANCE
                .AnyAsync(ub => ub.LICENSE_PLATE_NUMBER == licensePlate && ub.BALANCE >= 10);
        }

        public async Task<VehicleStatusResult?> GetVehicleStatusByLicensePlateAsync(string licensePlate)
        {
            return await _parkingContext.GetVehicleStatusByLicensePlate(licensePlate);
        }

        public async Task<List<ParkedVehicleInfo>> GetAllParkedVehiclesAsync(int areaId)
        {
            return await _parkingContext.GetAllParkedVehicles(areaId);
        }

        // 支付管理
        public async Task<List<VehicleExitResult>> GetUnpaidParkingRecordsAsync()
        {
            return await _parkingContext.GetUnpaidParkingRecords();
        }

        public async Task<List<VehicleExitResult>> GetPaymentRecordsAsync(string status = "all")
        {
            return await _parkingContext.GetPaymentRecords(status);
        }

        public async Task<bool> ProcessParkingPaymentAsync(ParkingPaymentRequest request)
        {
            return await _parkingContext.ProcessParkingPayment(request);
        }

        public async Task<List<ParkingPaymentRecord>> GetPaymentRecordsInTimeRangeAsync(DateTime start, DateTime end, int? areaId = null)
        {
            return await _parkingContext.GetPaymentRecordsInTimeRange(start, end, areaId);
        }

        // 统计报表
        public async Task<ParkingStatisticsReportResponseDto> GetParkingStatisticsReportAsync(DateTime startDate, DateTime endDate, int? areaId)
        {
            return await _parkingContext.GetParkingStatisticsReport(startDate, endDate, areaId);
        }

        public async Task<ParkingOperationStatistics> GetParkingOperationStatisticsAsync(DateTime startDate, DateTime endDate, int areaId)
        {
            return await _parkingContext.GetParkingOperationStatistics(startDate, endDate, areaId);
        }
    }
}