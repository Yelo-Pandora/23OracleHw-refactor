// IAreaComponent.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static oracle_backend.Models.CashFlowDto; // 引用 CashFlowRecord

namespace oracle_backend.patterns.Composite_Pattern.Component
{
    /// <summary>
    /// 区域组件接口 (Component)
    /// 统一了 Retail Area, Event Area, Parking Area, Other Area 四种区域在财务、运维、查询和校验上的共性行为。
    /// </summary>
    public interface IAreaComponent
    {
        // ==========================================
        // 1. 财务维度 (Financial)
        // ==========================================

        /// <summary>
        /// 获取现金流记录。
        /// 
        /// [逻辑来源]: 
        /// - RetailLeaf: 搬运 CashFlowController.GetRentalIncomesAsync (商铺租金)
        /// - ParkingLeaf: 搬运 CashFlowController.GetParkingIncomesAsync (停车费)
        /// - EventLeaf: 搬运 CashFlowController.GetEventSettlementsAsync (活动结算)
        /// - OtherLeaf: 返回空列表
        /// - Container: 汇总所有子节点的记录
        /// </summary>
        Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate);


        // ==========================================
        // 2. 信息维度 (Information & Status)
        // ==========================================

        /// <summary>
        /// 获取区域的详细信息快照。
        /// 这是一个"大一统"的 DTO，用于解决 AreaController.GetAreas 中 select new { ... } 的多态问题。
        /// 
        /// [逻辑来源]: 
        /// - AreaController.GetAreas (查询 BaseRent, Capacity, ParkingFee, Type)
        /// - ParkingController.GetParkingSummary (查询 OccupancyRate)
        /// </summary>
        Task<AreaComponentInfo> GetDetailsAsync();


        // ==========================================
        // 3. 业务变更 (Write)
        // 对应 AreaController.UpdateArea / ParkingController.UpdateParkingLotInfo
        // ==========================================

        /// <summary>
        /// 执行业务变更。
        /// 这是一个多态的更新操作。不同的 Leaf 会从配置对象中提取自己关心的字段。
        /// 
        /// RetailLeaf: 提取 RentStatus, BaseRent
        /// ParkingLeaf: 提取 Status, ParkingFee
        /// EventLeaf: 提取 Capacity, AreaFee
        /// OtherLeaf: 提取 Type
        /// </summary>
        /// <param name="config">统一的配置参数包</param>
        Task UpdateInfoAsync(AreaConfiguration config);


        // ==========================================
        // 4. 删除校验 (Validation)
        // 对应 AreaController.DeleteArea
        // ==========================================

        /// <summary>
        /// 询问组件：“我可以删除你吗？”
        /// 
        /// RetailLeaf: 检查 RentStore 表，如果有租户，返回 "已被店铺租用"。
        /// ParkingLeaf: 检查 Car 表，如果有车，返回 "仍有车辆在停"。
        /// EventLeaf: 检查 VenueEvent 表，如果有活动，返回 "有未结束的活动"。
        /// OtherLeaf: 返回 null (可以直接删除)。
        /// </summary>
        /// <returns>返回 null 表示可以删除；返回字符串表示具体的拒绝原因。</returns>
        Task<string?> ValidateDeleteConditionAsync();
    }
}