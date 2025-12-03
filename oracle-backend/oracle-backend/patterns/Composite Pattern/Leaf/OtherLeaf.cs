//OtherLeaf.cs
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models; // 引用 OtherArea, Area 模型
using oracle_backend.patterns.Composite_Pattern.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    /// <summary>
    /// 其他区域叶子节点 (如卫生间、走廊、杂物间)
    /// 逻辑搬运自: AreaController
    /// </summary>
    public class OtherLeaf : IAreaComponent
    {
        private readonly ComplexDbContext _context;
        private readonly int _areaId;

        public OtherLeaf(ComplexDbContext context, int areaId)
        {
            _context = context;
            _areaId = areaId;
        }

        // ==========================================
        // 1. 财务统计
        // 对应: CashFlowController (无 OTHER 类型处理逻辑)
        // ==========================================
        public Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // OtherArea 不产生收入，直接返回空列表
            return Task.FromResult(Enumerable.Empty<CashFlowRecord>());
        }

        // ==========================================
        // 2. 详情快照
        // 对应: AreaController.GetAreas / GetByID
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 1. 查找主表记录
            var area = await _context.Areas.FindAsync(_areaId);
            if (area == null) return null;

            // 2. 查找子表记录
            // 逻辑来源: AreaController.GetAreas 中的 .Select(ea => ea.TYPE)
            var otherArea = await _context.OtherAreas.FindAsync(_areaId);

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "OTHER",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                // OtherArea 没有特定占用率逻辑，简单映射 IsEmpty
                OccupancyRate = area.ISEMPTY == 0 ? 1.0 : 0.0,
                // 填充特有字段 TYPE
                SubType = otherArea?.TYPE ?? "未知",

                // 其他无关字段置空
                Price = null,
                BusinessStatus = null,
                CapacityOrSpaces = null
            };
        }

        // ==========================================
        // 3. 业务变更
        // 对应: AreaController.UpdateArea (case "OTHER")
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新主表 Area 属性
            var areaToUpdate = await _context.Areas.FindAsync(_areaId);
            if (areaToUpdate == null) return; // 或抛出异常

            if (config.IsEmpty.HasValue) areaToUpdate.ISEMPTY = config.IsEmpty.Value;
            if (config.AreaSize.HasValue) areaToUpdate.AREA_SIZE = config.AreaSize.Value;

            _context.Entry(areaToUpdate).State = EntityState.Modified;

            // 2. 更新或修复子表 OtherArea
            // 严格照抄 AreaController.UpdateArea 中的 switch case "OTHER" 逻辑
            var otherArea = await _context.OtherAreas.FindAsync(_areaId);

            if (otherArea == null)
            {
                // 逻辑搬运: "子表记录缺失，插入数据"
                // 这里的 SQL 插入是为了避免 EF Core 在 Area 存在时插入子类可能引发的主键冲突
                var type = config.TypeDescription ?? "未知";

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO OTHER_AREA (AREA_ID, TYPE) VALUES ({_areaId}, {type})");
            }
            else
            {
                // 逻辑搬运: "子表记录存在，执行更新"
                if (config.TypeDescription != null)
                {
                    otherArea.TYPE = config.TypeDescription;
                    _context.Entry(otherArea).State = EntityState.Modified;
                }
            }

            // 3. 提交事务 (主表更新和子表更新)
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 4. 删除校验
        // 对应: AreaController.DeleteArea
        // ==========================================
        public Task<string?> ValidateDeleteConditionAsync()
        {
            // AreaController.DeleteArea 中针对 "OTHER" 没有特定的子表外键检查逻辑。
            // (RentStores 是 Retail 的检查, VenueEvent 是 Event 的检查)
            // 所以这里直接返回 null，表示允许删除。
            return Task.FromResult<string?>(null);
        }
    }
}