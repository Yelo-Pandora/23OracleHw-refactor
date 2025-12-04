using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.patterns.Composite_Pattern.Component;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    public class ParkingLeaf : IAreaComponent
    {
        private readonly IAreaRepository _areaRepository;
        private readonly IParkingRepository _parkingRepository;
        private readonly int _areaId;

        public ParkingLeaf(IAreaRepository areaRepository, IParkingRepository parkingRepository, int areaId)
        {
            _areaRepository = areaRepository;
            _parkingRepository = parkingRepository;
            _areaId = areaId;
        }

        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // 1. 获取该区域下所有的车位ID
            var spaces = await _parkingRepository.GetParkingSpacesAsync(_areaId);
            var spaceIds = spaces.Select(s => s.PARKING_SPACE_ID).ToHashSet();

            // 2. 获取时间段内所有支付记录 (内存中)
            var allPayments = await _parkingRepository.GetPaymentRecordsInTimeRangeAsync(startDate, endDate);

            // 3. 筛选并转换
            var records = allPayments
                .Where(p => spaceIds.Contains(p.ParkingSpaceId)) // 关键筛选：只取本区域车位
                .Select(p => new CashFlowRecord
                {
                    Date = p.PaymentTime ?? p.ParkEnd ?? DateTime.Now,
                    Type = "收入",
                    Category = "停车场收费",
                    Description = $"车牌 {p.LicensePlateNumber} 停车费",
                    Amount = (double)p.TotalFee,
                    Reference = $"Parking-{p.LicensePlateNumber}-{p.ParkStart:yyyyMMddHHmm}",
                    RelatedPartyType = null
                });

            return records;
        }

        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            var parkingLot = await _areaRepository.GetParkingLotDetailAsync(_areaId);

            // 使用 ParkingRepo 的现有业务方法
            var stats = await _parkingRepository.GetParkingStatusStatisticsAsync(_areaId);
            var status = await _parkingRepository.GetParkingLotStatusAsync(_areaId);

            if (area == null) return null;

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "PARKING",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = stats.OccupancyRate / 100.0,
                Price = parkingLot?.PARKING_FEE,
                CapacityOrSpaces = stats.TotalSpaces,
                BusinessStatus = status,
                SubType = null
            };
        }

        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新基表 (Area)
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            if (area != null)
            {
                if (config.IsEmpty.HasValue) area.ISEMPTY = config.IsEmpty.Value;
                if (config.AreaSize.HasValue) area.AREA_SIZE = config.AreaSize.Value;
                _areaRepository.Update(area);
                await _areaRepository.SaveChangesAsync();
            }

            // 2. 更新子表 (ParkingLot) - 数据库层面
            await _areaRepository.UpsertParkingLotAsync(
                _areaId,
                config.Status ?? "正常运营",
                config.Price ?? 0);

            // 3. 同步到 ParkingRepository 的内存状态 (Status/Fee)
            await _parkingRepository.UpdateParkingLotInfoAsync(
                _areaId,
                (int)(config.Price ?? 0),
                config.Status ?? "正常运营");
        }

        public async Task<string?> ValidateDeleteConditionAsync()
        {
            return await _areaRepository.HasParkingDependencyAsync(_areaId)
                ? "无法删除：请先清理该停车场上的停车位。" : null;
        }
    }
}