//ParkingLeaf.cs
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
    /// 停车场区域叶子节点 (对应 PARKING_LOT)
    /// 封装 AreaController, ParkingController, CashFlowController 中针对停车场的业务
    /// </summary>
    public class ParkingLeaf : IAreaComponent
    {
        private readonly ParkingContext _context;
        private readonly int _areaId;

        public ParkingLeaf(ParkingContext context, int areaId)
        {
            _context = context;
            _areaId = areaId;
        }

        // ==========================================
        // 1. 财务统计
        // 逻辑来源: CashFlowController.GetParkingIncomesAsync
        // ==========================================
        public async Task<IEnumerable<CashFlowRecord>> GetCashFlowRecordsAsync(DateTime startDate, DateTime endDate)
        {
            // 逻辑搬运: 联表查询 CAR -> PARK -> PARKING_SPACE_DISTRIBUTION -> PARKING_LOT
            // 筛选出属于当前 AreaId 且在时间范围内的已完成停车记录
            var query = from c in _context.CAR
                        join p in _context.PARK on new { c.LICENSE_PLATE_NUMBER, c.PARK_START } equals new { p.LICENSE_PLATE_NUMBER, p.PARK_START }
                        join psd in _context.PARKING_SPACE_DISTRIBUTION on p.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                        join pl in _context.PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                        where psd.AREA_ID == _areaId
                           && c.PARK_END.HasValue
                           && c.PARK_END >= startDate
                           && c.PARK_END <= endDate
                        select new
                        {
                            c.LICENSE_PLATE_NUMBER,
                            c.PARK_START,
                            PARK_END = c.PARK_END.Value,
                            pl.PARKING_FEE,
                            psd.AREA_ID
                        };

            var dbRecords = await query.ToListAsync();
            var result = new List<CashFlowRecord>();

            foreach (var r in dbRecords)
            {
                // 计算逻辑搬运: 时长向上取整 * 费率
                var duration = r.PARK_END - r.PARK_START;
                var hours = Math.Ceiling(duration.TotalHours);
                var amount = hours * (double)r.PARKING_FEE;

                result.Add(new CashFlowRecord
                {
                    Date = r.PARK_END, // 以出场时间结算
                    Type = "收入",
                    Category = "停车场收费", // 对应 CashFlowController 的模块类型
                    Description = $"车辆 {r.LICENSE_PLATE_NUMBER} 在区域{r.AREA_ID}停车费支付",
                    Amount = amount,
                    Reference = $"停车支付-{r.LICENSE_PLATE_NUMBER}-{r.PARK_END:yyyyMMddHHmmss}",
                    RelatedPartyType = null,
                    RelatedPartyId = null
                });
            }

            return result;
        }

        // ==========================================
        // 2. 详情快照
        // 逻辑来源: AreaController.GetAreas / ParkingController.GetParkingLotInfo
        // ==========================================
        public async Task<AreaComponentInfo> GetDetailsAsync()
        {
            // 1. 查找主表
            var area = await _context.AREA.FindAsync(_areaId);
            if (area == null) return null;

            // 2. 获取停车场专用统计信息 (逻辑搬运自 ParkingController)
            // GetParkingStatusStatistics 内部计算了 OccupiedSpaces / TotalSpaces
            var stats = await _context.GetParkingStatusStatistics(_areaId);

            // 3. 获取停车场专用状态 (逻辑搬运自 ParkingController)
            // 状态存储在 ParkingContext 的静态字典中
            var status = _context.GetParkingLotStatus(_areaId);

            // 4. 获取停车场基本信息 (费率)
            var parkingLot = await _context.GetParkingLotById(_areaId);

            return new AreaComponentInfo
            {
                AreaId = area.AREA_ID,
                Category = "PARKING",
                IsEmpty = area.ISEMPTY,
                AreaSize = area.AREA_SIZE,

                // 占用率归一化 (0.0 - 1.0)
                OccupancyRate = stats.OccupancyRate / 100.0,

                // 填充 Parking 特有字段
                Price = parkingLot?.PARKING_FEE,      // ParkingFee -> Price
                CapacityOrSpaces = stats.TotalSpaces, // TotalSpaces -> CapacityOrSpaces
                BusinessStatus = status,              // "正常运营", "维护中" -> BusinessStatus

                SubType = null
            };
        }

        // ==========================================
        // 3. 业务变更
        // 逻辑来源: AreaController.UpdateArea (case "PARKING") + ParkingController.UpdateParkingLotInfo
        // ==========================================
        public async Task UpdateInfoAsync(AreaConfiguration config)
        {
            // 1. 更新主表 AREA (EF Core)
            var areaToUpdate = await _context.AREA.FindAsync(_areaId);
            if (areaToUpdate != null)
            {
                if (config.IsEmpty.HasValue) areaToUpdate.ISEMPTY = config.IsEmpty.Value;
                if (config.AreaSize.HasValue) areaToUpdate.AREA_SIZE = config.AreaSize.Value;
                _context.Entry(areaToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            // 2. 更新 PARKING_LOT 表及内存状态
            // 这里我们复用 ParkingContext.UpdateParkingLotInfo 方法，它封装了 DB 更新和内存状态更新
            // 注意：如果表记录不存在，需要先处理插入逻辑 (参考 AreaController)

            var parkingLotExists = await _context.ParkingLotExists(_areaId);
            int fee = config.Price.HasValue ? (int)config.Price.Value : 0;
            string status = config.Status ?? "正常运营";

            if (!parkingLotExists)
            {
                // 逻辑搬运: 子表缺失，执行 SQL 插入
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO PARKING_LOT (AREA_ID, PARKING_FEE) VALUES ({_areaId}, {fee})");

                // 插入后调用 Update 确保内存状态同步
                await _context.UpdateParkingLotInfo(_areaId, fee, status);
            }
            else
            {
                // 逻辑搬运: 子表存在，执行更新
                // 获取当前费率以防 config.Price 为空时被覆盖为 0
                if (!config.Price.HasValue)
                {
                    var current = await _context.GetParkingLotById(_areaId);
                    fee = current?.PARKING_FEE ?? 0;
                }

                // 调用 ParkingContext 封装的方法更新 DB 和 内存状态
                await _context.UpdateParkingLotInfo(_areaId, fee, status);
            }
        }

        // ==========================================
        // 4. 删除校验
        // 逻辑来源: AreaController.DeleteArea
        // ==========================================
        public async Task<string?> ValidateDeleteConditionAsync()
        {
            // 逻辑搬运: "无法删除：请先清理该停车场上的停车位。"
            // 检查 PARKING_SPACE_DISTRIBUTION 表
            var hasParkingSpaces = await _context.PARKING_SPACE_DISTRIBUTION
                .FirstOrDefaultAsync(rs => rs.AREA_ID == _areaId);

            if (hasParkingSpaces != null)
            {
                return "无法删除：请先清理该停车场上的停车位。";
            }

            return null; // 允许删除
        }
    }
}
