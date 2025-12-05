//AreaDTOs.cs
namespace oracle_backend.patterns.Composite_Pattern.Component
{
    /// <summary>
    /// [读模型] 统一返回给前端的详情
    /// 包含了所有类型可能用到的字段，由 Leaf 负责填充
    /// </summary>
    public class AreaComponentInfo
    {
        // 通用字段 (Area 表)
        public int AreaId { get; set; }
        public string Category { get; set; } // "RETAIL", "PARKING", "EVENT", "OTHER", "CONTAINER" 
        public int IsEmpty { get; set; }
        public int? AreaSize { get; set; }
        
        // 动态计算字段
        public double OccupancyRate { get; set; } // 占用率 (Parking/Retail/Event 通用概念)

        // 差异化字段 (Leaf 根据自身类型填充，其他为 null)
        public double? Price { get; set; }        // 统一代表：BaseRent / ParkingFee / AreaFee
        public string? BusinessStatus { get; set; } // 统一代表：RentStatus / StoreStatus / ParkingStatus
        public int? CapacityOrSpaces { get; set; }  // 统一代表：EventCapacity / TotalParkingSpaces
        public string? SubType { get; set; }        // 统一代表：OtherArea.Type
    }

    /// <summary>
    /// [写模型] Controller 传给 Leaf 的更新参数包
    /// </summary>
    public class AreaConfiguration
    {
        // 对应 AreaController.UpdateArea 的入参
        public int? IsEmpty { get; set; }
        public int? AreaSize { get; set; }
        
        // 关键的多态字段
        // 如果是改商铺，Controller 把租金填入 Price
        // 如果是改停车，Controller 把停车费填入 Price
        public double? Price { get; set; } 
        
        // 如果是改商铺，Controller 把 "装修中" 填入 Status
        // 如果是改停车，Controller 把 "维护中" 填入 Status
        public string? Status { get; set; }
        
        public int? Capacity { get; set; } // 容量/车位数
        public string? TypeDescription { get; set; } // 针对 OtherArea 的描述
    }
}