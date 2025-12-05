using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.patterns.Composite_Pattern.Component;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    public class RetailLeaf : IAreaComponent
    {
        private readonly IAreaRepository _areaRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly int _areaId;

        public RetailLeaf(IAreaRepository areaRepository, IStoreRepository storeRepository, int areaId)
        {
            _areaRepository = areaRepository;
            _storeRepository = storeRepository;
            _areaId = areaId;
        }

        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            var records = new List<CashFlowRecord>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);

            // 遍历月份
            while (current <= endDate || (current.Year == endDate.Year && current.Month == endDate.Month))
            {
                string period = current.ToString("yyyyMM");

                // 调用 StoreRepository 获取当月所有记录
                var rentDetails = await _storeRepository.GetRentCollectionDetailsAsync(period);

                // 在内存中筛选：必须是当前 AreaID
                var validRecords = rentDetails
                    .Where(d => d.AreaId == _areaId && // 关键筛选
                                d.Status == "已缴纳" &&
                                d.PaymentDate.HasValue &&
                                d.PaymentDate.Value >= startDate &&
                                d.PaymentDate.Value <= endDate)
                    .Select(d => new CashFlowRecord
                    {
                        Date = d.PaymentDate.Value,
                        Type = "收入",
                        Category = "商户租金",
                        Description = $"商户{d.StoreName}租金",
                        Amount = (double)d.ActualAmount,
                        Reference = $"Rent-{d.StoreId}-{d.Period}",
                        RelatedPartyType = "商户",
                        RelatedPartyId = d.StoreId,
                    });

                records.AddRange(validRecords);
                current = current.AddMonths(1);
                if (current > endDate) break;
            }

            return records;
        }

        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            var retail = await _areaRepository.GetRetailAreaDetailAsync(_areaId);
            if (area == null) return null;

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "RETAIL",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = area.ISEMPTY == 0 ? 1.0 : 0.0,
                Price = retail?.BASE_RENT,
                BusinessStatus = retail?.RENT_STATUS,
                CapacityOrSpaces = null,
                SubType = null
            };
        }

        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            if (area != null)
            {
                if (config.IsEmpty.HasValue) area.ISEMPTY = config.IsEmpty.Value;
                if (config.AreaSize.HasValue) area.AREA_SIZE = config.AreaSize.Value;
                _areaRepository.Update(area);
                await _areaRepository.SaveChangesAsync();
            }

            await _areaRepository.UpsertRetailAreaAsync(
                _areaId,
                config.Status ?? "正常营业",
                config.Price ?? 0);
        }

        public async Task<string?> ValidateDeleteConditionAsync()
        {
            return await _areaRepository.HasRetailDependencyAsync(_areaId)
                ? "无法删除：该区域已被店铺租用。" : null;
        }
    }
}