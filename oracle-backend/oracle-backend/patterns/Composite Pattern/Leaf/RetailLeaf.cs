//RetailLeaf.cs
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    /// <summary>
    /// 零售/商铺区域叶子节点 (对应 RETAIL_AREA)
    /// 封装 AreaController, StoreController, CashFlowController 中针对商铺的操作
    /// </summary>
    public class RetailLeaf : IAreaComponent
    {
        private readonly StoreDbContext _context;
        private readonly int _areaId;

        public RetailLeaf(StoreDbContext context, int areaId)
        {
            _context = context;
            _areaId = areaId;
        }

        // ==========================================
        // 1. 财务统计
        // 逻辑来源: CashFlowController.GetRentalIncomesAsync
        // ==========================================
        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            var records = new List<CashFlowRecord>();

            // 逻辑搬运: 遍历起止日期之间的月份，调用 StoreDbContext 的专用方法
            // CashFlowController 中 GetPeriodsInRange 的逻辑内联在此
            var current = new DateTime(startDate.Year, startDate.Month, 1);

            // 修正：循环条件应包含结束月份
            // 假设 endDate 是 2023-10-15，current 是 2023-10-01，应当处理 10 月
            while (current <= endDate || (current.Year == endDate.Year && current.Month == endDate.Month))
            {
                string period = current.ToString("yyyyMM");

                // 调用 StoreDbContext 中的复杂查询逻辑
                var rentDetails = await _context.GetRentCollectionDetails(period);

                // 筛选逻辑搬运: Status == "已缴纳" && PaymentDate 在范围内 && 属于当前 Area
                var validRecords = rentDetails
                    .Where(d => d.AreaId == _areaId && // 关键：只筛选当前 Leaf 对应的区域
                                d.Status == "已缴纳" &&
                                d.PaymentDate.HasValue &&
                                d.PaymentDate.Value >= startDate &&
                                d.PaymentDate.Value <= endDate)
                    .Select(d => new CashFlowRecord
                    {
                        Date = d.PaymentDate.Value,
                        Type = "收入",
                        Category = "商户租金", // 对应 CashFlowController 的模块类型
                        Description = $"商户{d.StoreName}租金",
                        Amount = (double)d.ActualAmount,
                        Reference = $"Rent-{d.StoreId}-{d.Period}",
                        RelatedPartyType = "商户",
                        RelatedPartyId = d.StoreId,
                    });

                records.AddRange(validRecords);

                // 移动到下一个月
                current = current.AddMonths(1);

                // 防止死循环，如果 current 已经超过 endDate 且月份不同，则退出
                if (current > endDate) break;
            }

            return records;
        }

        // ==========================================
        // 2. 详情快照
        // 逻辑来源: AreaController.GetAreas (case "RETAIL")
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 1. 查找主表
            var area = await _context.AREA.FindAsync(_areaId);
            if (area == null) return null;

            // 2. 查找子表 (RetailArea)
            var retailArea = await _context.RETAIL_AREA.FindAsync(_areaId);

            // 3. 计算占用率
            // StoreController.GetBasicStatistics 中的逻辑：VacantAreas = (ISEMPTY == 1)
            double occupancy = area.ISEMPTY == 0 ? 1.0 : 0.0;

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "RETAIL",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = occupancy,

                // 填充 Retail 特有字段
                // BaseRent -> Price
                Price = retailArea?.BASE_RENT,
                // RentStatus -> BusinessStatus
                BusinessStatus = retailArea?.RENT_STATUS,

                // 其他无关字段置空
                CapacityOrSpaces = null,
                SubType = null
            };
        }

        // ==========================================
        // 3. 业务变更
        // 逻辑来源: AreaController.UpdateArea (case "RETAIL")
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新主表 Area
            var areaToUpdate = await _context.AREA.FindAsync(_areaId);
            if (areaToUpdate == null) return;

            if (config.IsEmpty.HasValue) areaToUpdate.ISEMPTY = config.IsEmpty.Value;
            if (config.AreaSize.HasValue) areaToUpdate.AREA_SIZE = config.AreaSize.Value;
            _context.Entry(areaToUpdate).State = EntityState.Modified;

            // 2. 更新或修复子表 RetailArea
            // 严格照抄 AreaController.UpdateArea 中的 switch case "RETAIL" 逻辑
            var retailArea = await _context.RETAIL_AREA.FindAsync(_areaId);

            if (retailArea == null)
            {
                // 逻辑搬运: "子表记录缺失，插入数据"
                // 使用原生 SQL 插入以处理 EF Core TPT 问题
                var rentStatus = config.Status ?? "正常营业"; // 对应 RENT_STATUS
                var baseRent = config.Price ?? 0;           // 对应 BASE_RENT

                // 注意：StoreDbContext 也有 Database 属性
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO RETAIL_AREA (AREA_ID, RENT_STATUS, BASE_RENT) VALUES ({_areaId}, {rentStatus}, {baseRent})");
            }
            else
            {
                // 逻辑搬运: "子表记录存在，执行更新"
                if (config.Status != null)
                {
                    retailArea.RENT_STATUS = config.Status;
                }
                if (config.Price.HasValue)
                {
                    retailArea.BASE_RENT = config.Price.Value;
                }
                _context.Entry(retailArea).State = EntityState.Modified;
            }

            // 3. 提交事务
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 4. 删除校验
        // 逻辑来源: AreaController.DeleteArea
        // ==========================================
        public async Task<string?> ValidateDeleteConditionAsync()
        {
            // 逻辑搬运: "无法删除：该区域已被店铺租用。"
            // 检查 RentStore 表中是否存在该 AreaId 的记录
            var hasActiveRent = await _context.RENT_STORE
                .FirstOrDefaultAsync(rs => rs.AREA_ID == _areaId);

            if (hasActiveRent != null)
            {
                return "无法删除：该区域已被店铺租用。";
            }

            return null; // 允许删除
        }
    }
}