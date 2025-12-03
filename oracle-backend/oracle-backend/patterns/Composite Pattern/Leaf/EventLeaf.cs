//EventLeaf.cs
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
    /// 活动区域叶子节点 (对应 EVENT_AREA)
    /// 封装 AreaController 和 VenueEventController 中针对活动场地的操作
    /// </summary>
    public class EventLeaf : IAreaComponent
    {
        private readonly ComplexDbContext _context;
        private readonly int _areaId;

        public EventLeaf(ComplexDbContext context, int areaId)
        {
            _context = context;
            _areaId = areaId;
        }

        // ==========================================
        // 1. 财务统计
        // 逻辑来源: CashFlowController.GetEventSettlementsAsync
        // ==========================================
        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // 逻辑搬运: 筛选 "已结算" 且时间符合范围的活动记录
            // 注意：CashFlowController 使用的是 ved.RENT_END >= startDate && ved.RENT_START <= endDate 这种范围重叠查询
            // 且 Date 字段取的是 RENT_END
            var eventsQuery = _context.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Where(ved => ved.AREA_ID == _areaId &&
                              ved.STATUS == "已结算" &&
                              ved.RENT_END >= startDate &&
                              ved.RENT_START <= endDate);

            var eventList = await eventsQuery.ToListAsync();

            var records = eventList.Select(r => new CashFlowRecord
            {
                Date = r.RENT_END, // 结算时间通常视为结束时间
                Type = "收入",
                Category = "场地活动", // 对应 CashFlowController 中的模块类型
                Description = $"活动{r.EVENT_ID}结算",
                Amount = r.FUNDING,
                Reference = $"VenueEventDetails-{r.EVENT_ID}-{r.RENT_START:yyyyMMddHHmmss}",
                RelatedPartyType = "合作方",
                RelatedPartyId = r.COLLABORATION_ID
            });

            return records;
        }

        // ==========================================
        // 2. 详情快照
        // 逻辑来源: AreaController.GetAreas (case "EVENT")
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 1. 查找主表
            var area = await _context.Areas.FindAsync(_areaId);
            if (area == null) return null;

            // 2. 查找子表 (EventArea)
            // 逻辑来源: AreaController 中 Select 查询 EventAreas 部分
            var eventArea = await _context.EventAreas.FindAsync(_areaId);

            // 3. 计算占用情况 (VenueEventController 中有类似逻辑)
            // 简单逻辑：如果 IsEmpty == 0，则认为占用率为 100% (或者可以根据活动预约情况更精细计算，这里保持与Controller一致的基础逻辑)
            double occupancy = area.ISEMPTY == 0 ? 1.0 : 0.0;

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "EVENT",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = occupancy,

                // 填充 Event 特有字段
                // AreaFee -> Price
                Price = eventArea?.AREA_FEE != null ? (double)eventArea.AREA_FEE : null,
                // Capacity -> CapacityOrSpaces
                CapacityOrSpaces = eventArea?.CAPACITY,

                // 其他无关字段置空
                BusinessStatus = null, // EventArea 没有单独的状态字段，状态通常在 VenueEventDetails 中
                SubType = null
            };
        }

        // ==========================================
        // 3. 业务变更
        // 逻辑来源: AreaController.UpdateArea (case "EVENT")
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新主表 Area
            var areaToUpdate = await _context.Areas.FindAsync(_areaId);
            if (areaToUpdate == null) return;

            if (config.IsEmpty.HasValue) areaToUpdate.ISEMPTY = config.IsEmpty.Value;
            if (config.AreaSize.HasValue) areaToUpdate.AREA_SIZE = config.AreaSize.Value;
            _context.Entry(areaToUpdate).State = EntityState.Modified;

            // 2. 更新或修复子表 EventArea
            // 严格照抄 AreaController.UpdateArea 中的 switch case "EVENT" 逻辑
            var eventArea = await _context.EventAreas.FindAsync(_areaId);

            if (eventArea == null)
            {
                // 逻辑搬运: "子表记录缺失，插入数据"
                // 避免 EF Core TPT 主键冲突，使用原生 SQL
                var capacity = config.Capacity ?? 0;
                var areaFee = config.Price ?? 0; // Config.Price 对应 AreaFee

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO EVENT_AREA (AREA_ID, CAPACITY, AREA_FEE) VALUES ({_areaId}, {capacity}, {areaFee})");
            }
            else
            {
                // 逻辑搬运: "子表记录存在，执行更新"
                if (config.Capacity.HasValue)
                {
                    eventArea.CAPACITY = config.Capacity.Value;
                }
                if (config.Price.HasValue)
                {
                    eventArea.AREA_FEE = (int)config.Price.Value; // 注意转换 double -> int
                }
                _context.Entry(eventArea).State = EntityState.Modified;
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
            // 逻辑搬运: "无法删除：该区域已有关联的场地活动。"
            // 检查 VenueEventDetails 表中是否存在该 AreaId 的记录
            var hasActiveEvent = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => ved.AREA_ID == _areaId);

            if (hasActiveEvent != null)
            {
                return "无法删除：该区域已有关联的场地活动。";
            }

            return null; // 允许删除
        }
    }
}
