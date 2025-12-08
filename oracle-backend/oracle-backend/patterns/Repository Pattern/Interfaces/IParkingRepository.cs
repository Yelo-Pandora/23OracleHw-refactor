using oracle_backend.Models;
using static oracle_backend.Controllers.ParkingController; // 引用 Controller 中定义的 DTO

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IParkingRepository : IRepository<ParkingLot>
    {
        // --- 1. 停车场信息管理 ---
        Task<bool> ParkingLotExistsAsync(int areaId);
        Task<ParkingLot?> GetParkingLotByIdAsync(int areaId);
        Task<bool> UpdateParkingLotInfoAsync(int areaId, int parkingFee, string status, int? parkingSpaceCount = null);

        // 注意：原 Context 中是同步方法，这里封装为 Async 以保持 Repository 风格一致性
        Task<string> GetParkingLotStatusAsync(int areaId);
        Task<int> GetParkingLotSpaceCountAsync(int areaId);
        Task<List<dynamic>> GetParkingLotListAsync();

        // --- 2. 车位管理 ---
        Task<List<ParkingSpace>> GetParkingSpacesAsync(int? areaId = null);
        Task<ParkingStatusStatistics> GetParkingStatusStatisticsAsync(int areaId);
        Task<List<ParkingSpaceStatus>> GetParkingSpaceStatusesAsync(int areaId);
        Task<bool> IsParkingSpaceAvailableAsync(int parkingSpaceId);

        // --- 3. 车辆管理 (入场/出场/查询) ---
        Task<(bool Success, int ErrorCode, string Message)> VehicleEntryAsync(string licensePlate, int spaceId);
        Task<VehicleExitResult?> VehicleExitAsync(string licensePlate);
        
        // 责任链处理者所需方法
        Task<bool> IsVehicleBlacklistedAsync(string licensePlate);
        Task<bool> HasSufficientBalanceAsync(string licensePlate);

        Task<List<VehicleStatusResult>> GetCurrentVehiclesAsync(int? areaId = null);
        Task<VehicleStatusResult?> GetVehicleStatusByLicensePlateAsync(string licensePlate);
        Task<List<ParkedVehicleInfo>> GetAllParkedVehiclesAsync(int areaId);

        // --- 4. 支付管理 ---
        Task<List<VehicleExitResult>> GetUnpaidParkingRecordsAsync();
        Task<List<VehicleExitResult>> GetPaymentRecordsAsync(string status = "all");
        Task<bool> ProcessParkingPaymentAsync(ParkingPaymentRequest request);

        // 专门用于报表或修复数据的支付记录查询
        Task<List<ParkingPaymentRecord>> GetPaymentRecordsInTimeRangeAsync(DateTime start, DateTime end, int? areaId = null);

        // --- 5. 统计报表 ---
        Task<ParkingStatisticsReportResponseDto> GetParkingStatisticsReportAsync(DateTime startDate, DateTime endDate, int? areaId);
        Task<ParkingOperationStatistics> GetParkingOperationStatisticsAsync(DateTime startDate, DateTime endDate, int areaId);
    }
}