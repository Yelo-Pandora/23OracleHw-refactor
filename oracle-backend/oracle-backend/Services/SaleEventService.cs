using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using oracle_backend.Models;
using Microsoft.Extensions.Logging;
using oracle_backend.Patterns.Repository.Interfaces; // 引入 Repository 接口

namespace oracle_backend.Services
{
    public interface ISaleEventService
    {
        Task<List<SaleEvent>> GetAllSaleEventsAsync();
        Task<SaleEvent> GetSaleEventAsync(int id);
        Task<SaleEvent> CreateSaleEventAsync(SaleEventDto dto);
        Task<SaleEvent> UpdateSaleEventAsync(int id, SaleEventDto dto);
        Task<bool> DeleteSaleEventAsync(int id);
        Task<SaleEventReport> GenerateSaleEventReportAsync(int eventId);

        Task AddStoreToEventAsync(int eventId, int storeId);
        Task RemoveStoreFromEventAsync(int eventId, int storeId);
        Task<List<Store>> GetStoresByEventAsync(int eventId);
        Task<List<SaleEvent>> GetEventsByStoreAsync(int storeId);
    }

    public class SaleEventService : ISaleEventService
    {
        // 依赖注入 Repository 而不是 DbContext
        private readonly ISaleEventRepository _repo;
        private readonly ILogger<SaleEventService> _logger;

        public SaleEventService(ISaleEventRepository repo, ILogger<SaleEventService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<List<SaleEvent>> GetAllSaleEventsAsync()
        {
            try
            {
                _logger.LogInformation("开始获取所有促销活动");
                // 调用 BaseRepository 的 GetAllAsync
                var result = await _repo.GetAllAsync();

                // 注意：BaseRepository 返回的是 IEnumerable，这里转为 List 以匹配接口
                var list = result.ToList();

                _logger.LogInformation($"成功获取 {list.Count} 条促销活动记录");
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取促销活动列表时出错: {ex.Message}");
                throw;
            }
        }

        public async Task<SaleEvent> CreateSaleEventAsync(SaleEventDto dto)
        {
            if (dto.EventStart > dto.EventEnd)
                throw new ArgumentException("结束时间必须晚于开始时间");

            // 业务逻辑：手动生成 ID
            int maxId = await _repo.GetMaxEventIdAsync();
            int newId = maxId + 1;

            var saleEvent = new SaleEvent
            {
                EVENT_ID = newId,
                EVENT_NAME = dto.EventName,
                Cost = dto.Cost,
                EVENT_START = dto.EventStart,
                EVENT_END = dto.EventEnd,
                Description = dto.Description,
            };

            await _repo.AddAsync(saleEvent);
            await _repo.SaveChangesAsync();

            return saleEvent;
        }

        public async Task<SaleEvent> UpdateSaleEventAsync(int id, SaleEventDto dto)
        {
            var saleEvent = await _repo.GetByIdAsync(id);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 更新字段业务逻辑
            if (!string.IsNullOrEmpty(dto.EventName))
                saleEvent.EVENT_NAME = dto.EventName;

            if (dto.Cost > 0)
                saleEvent.Cost = dto.Cost;

            if (!string.IsNullOrEmpty(dto.Description))
                saleEvent.Description = dto.Description;

            if (dto.EventStart != default)
                saleEvent.EVENT_START = dto.EventStart;

            if (dto.EventEnd != default)
                saleEvent.EVENT_END = dto.EventEnd;

            _repo.Update(saleEvent);
            await _repo.SaveChangesAsync();
            return saleEvent;
        }

        public async Task<bool> DeleteSaleEventAsync(int id)
        {
            var saleEvent = await _repo.GetByIdAsync(id);
            if (saleEvent == null) return false;

            _repo.Remove(saleEvent);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<SaleEvent> GetSaleEventAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<SaleEventReport> GenerateSaleEventReportAsync(int eventId)
        {
            var saleEvent = await _repo.GetByIdAsync(eventId);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 模拟调用外部系统获取数据
            var reportData = await FetchSalesDataFromExternalSystem(saleEvent);

            return new SaleEventReport
            {
                EventId = saleEvent.EVENT_ID,
                EventName = saleEvent.EVENT_NAME,
                SalesIncrement = reportData.SalesIncrement,
                Cost = saleEvent.Cost,
                ROI = CalculateROI(reportData.SalesIncrement, saleEvent.Cost),
                CouponRedemptionRate = reportData.CouponRedemptionRate
            };
        }

        public async Task AddStoreToEventAsync(int eventId, int storeId)
        {
            // 检查关联是否存在
            var existing = await _repo.GetPartStoreAsync(eventId, storeId);

            if (existing != null)
                throw new Exception("商铺已参与该活动");

            var partStore = new PartStore
            {
                EVENT_ID = eventId,
                STORE_ID = storeId
            };

            await _repo.AddPartStoreAsync(partStore);
            await _repo.SaveChangesAsync();
        }

        public async Task RemoveStoreFromEventAsync(int eventId, int storeId)
        {
            var partStore = await _repo.GetPartStoreAsync(eventId, storeId);

            if (partStore == null)
                throw new Exception("未找到关联记录");

            _repo.RemovePartStore(partStore);
            await _repo.SaveChangesAsync();
        }

        public async Task<List<Store>> GetStoresByEventAsync(int eventId)
        {
            // Join 逻辑已封装在 Repository 中
            return await _repo.GetStoresByEventIdAsync(eventId);
        }

        public async Task<List<SaleEvent>> GetEventsByStoreAsync(int storeId)
        {
            // Join 逻辑已封装在 Repository 中
            return await _repo.GetEventsByStoreIdAsync(storeId);
        }

        // --- 私有辅助方法保持不变 ---

        private double CalculateROI(double salesIncrement, double cost)
        {
            return cost == 0 ? 0 : (salesIncrement - cost) / cost;
        }

        private async Task<(double SalesIncrement, double CouponRedemptionRate)>
            FetchSalesDataFromExternalSystem(SaleEvent saleEvent)
        {
            await Task.Delay(100);
            return (
                SalesIncrement: new Random().Next(1000, 10000),
                CouponRedemptionRate: new Random().Next(50, 95) / 100.0
            );
        }
    }
}