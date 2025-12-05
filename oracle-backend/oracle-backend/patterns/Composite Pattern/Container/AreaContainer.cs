//AreaContainer.cs
using oracle_backend.patterns.Composite_Pattern.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static oracle_backend.Models.CashFlowDto;

namespace oracle_backend.patterns.Composite_Pattern.Container
{
    /// <summary>
    /// 区域容器 (Container)
    /// 职责：将对“整体”的请求，分发给内部所有的“部分” (Leaf)，并将结果合并返回给 Controller。
    /// </summary>
    public class AreaContainer : IAreaComponent
    {
        private readonly List<IAreaComponent> _children = new List<IAreaComponent>();
        private readonly string _groupName; // 例如 "全商城" 或 "A区"

        public AreaContainer(string groupName)
        {
            _groupName = groupName;
        }

        public void Add(IAreaComponent component) => _children.Add(component);
        public void Remove(IAreaComponent component) => _children.Remove(component);

        // ==========================================
        // 1. 财务统计
        // 对应 CashFlowController
        // 需求：Controller 需要拿到所有模块的 List<CashFlowRecord>，然后自己去 GroupBy 或 Sum。
        // Container 动作：【列表合并】(FlatMap / SelectMany)
        // ==========================================
        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            var allRecords = new List<CashFlowRecord>();

            foreach (var child in _children)
            {
                // 分发给每个 Leaf (Parking, Retail, Event) 去查自己的表
                var childRecords = await child.GetCashFlowRecordsAsync(startDate, endDate);
                // 汇总到一个大列表中
                allRecords.AddRange(childRecords);
            }

            return allRecords;
        }

        // ==========================================
        // 2. 详情快照
        // 对应 AreaController / ParkingController
        // 需求：通常 Controller 查的是具体 Leaf 的详情。
        // 但如果 Controller 想要“全商场概况”，Container 需要返回一个代表“整体”的对象。
        // Container 动作：【状态聚合】(最基础的汇总，不通过计算复杂指标)
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 简单统计子节点数量，不做复杂的加权平均，避免业务逻辑泄露到 Container
            int totalCount = _children.Count;

            // 仅仅作为一个容器的描述返回
            // 具体的业务计算交由 Controller 拿到 List 后自己处理
            return await Task.FromResult(new AreaComponentInfo
            {
                AreaId = -1, // 虚拟ID，表示这是一个 Container 而不是 Leaf
                Category = "CONTAINER",
                SubType = _groupName,
                BusinessStatus = $"包含 {totalCount} 个区域",

                // 容器本身没有具体的“租金”或“面积”，置空或设为0
                // 如果客户端需要总面积，应该遍历 Children 自己累加
                Price = null,
                AreaSize = null,
                OccupancyRate = 0,
                IsEmpty = totalCount == 0 ? 1 : 0
            });
        }

        // ==========================================
        // 3. 业务变更
        // 对应 AreaController.UpdateArea / ParkingController
        // 需求：比如“一键将所有区域设为维护中”。
        // Container 动作：【广播】
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 将更新指令原样传递给每一个子节点
            // 例如：Controller 发令 "全场维护"，Container 负责传达给每一个铺位和停车场
            foreach (var child in _children)
            {
                await child.UpdateInfoAsync(config);
            }
        }

        // ==========================================
        // 4. 删除校验
        // 对应 AreaController.DeleteArea
        // 需求：如果要删除一个楼层(Container)，必须确保楼层里没有正在营业的店铺。
        // Container 动作：【递归检查】
        // ==========================================
        public async Task<string?> ValidateDeleteConditionAsync()
        {
            // 只要有一个孩子说“不行”，整个容器就不能被删除
            foreach (var child in _children)
            {
                string? error = await child.ValidateDeleteConditionAsync();
                if (error != null)
                {
                    // 只要发现一个阻挡，立马向上汇报，不需要继续检查
                    return $"容器[{_groupName}]内存在不可删除的节点: {error}";
                }
            }

            return null; // 所有孩子都说可以删，那我也可以删
        }
    }
}
