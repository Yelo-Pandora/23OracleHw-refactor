using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.patterns.Composite_Pattern.Component;
using oracle_backend.Models;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    /// <summary>
    /// 其他区域叶子节点 (对应 OTHER_AREA)
    /// 重构后：完全依赖 IAreaRepository
    /// </summary>
    public class OtherLeaf : IAreaComponent
    {
        private readonly IAreaRepository _areaRepository;
        private readonly int _areaId;

        public OtherLeaf(IAreaRepository areaRepository, int areaId)
        {
            _areaRepository = areaRepository;
            _areaId = areaId;
        }

        // ==========================================
        // 1. 财务统计
        // ==========================================
        public Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // OtherArea 不产生直接收入
            return Task.FromResult(Enumerable.Empty<CashFlowRecord>());
        }

        // ==========================================
        // 2. 详情快照
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 1. 获取基表信息
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            if (area == null) return null;

            // 2. 获取子表信息
            var otherArea = await _areaRepository.GetOtherAreaDetailAsync(_areaId);

            // 3. 组装数据
            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "OTHER",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,
                OccupancyRate = area.ISEMPTY == 0 ? 1.0 : 0.0,

                // 填充特有字段 TYPE -> SubType
                SubType = otherArea?.TYPE ?? "未知",

                // 其他无关字段
                Price = null,
                BusinessStatus = null,
                CapacityOrSpaces = null
            };
        }

        // ==========================================
        // 3. 业务变更
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新主表 (Area)
            var area = await _areaRepository.GetAreaByIdAsync(_areaId);
            if (area != null)
            {
                if (config.IsEmpty.HasValue) area.ISEMPTY = config.IsEmpty.Value;
                if (config.AreaSize.HasValue) area.AREA_SIZE = config.AreaSize.Value;

                _areaRepository.Update(area);
                await _areaRepository.SaveChangesAsync();
            }

            // 2. 更新或插入子表 (OtherArea)
            // Repository 处理 SQL 插入或更新逻辑
            await _areaRepository.UpsertOtherAreaAsync(
                _areaId,
                config.TypeDescription ?? "未知"
            );
        }

        // ==========================================
        // 4. 删除校验
        // ==========================================
        public Task<string?> ValidateDeleteConditionAsync()
        {
            // Other 类型通常没有强外键依赖约束，或者没有复杂的业务逻辑阻挡删除
            // 如果未来有，可以在 IAreaRepository 增加 HasOtherDependencyAsync
            return Task.FromResult<string?>(null);
        }
    }
}