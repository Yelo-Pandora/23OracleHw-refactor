using Microsoft.AspNetCore.Mvc;
using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using oracle_backend.patterns.Composite_Pattern.Leaf;
using oracle_backend.Patterns.Repository.Implementations;
using oracle_backend.Patterns.Repository.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreasController : ControllerBase
    {
        private readonly IAreaRepository _areaRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IParkingRepository _parkingRepository;
        private readonly IVenueEventRepository _venueEventRepository;
        private readonly ILogger<AreasController> _logger;

        public AreasController(
            IAreaRepository areaRepository,
            IStoreRepository storeRepository,
            IParkingRepository parkingRepository,
            IVenueEventRepository venueEventRepository,
            ILogger<AreasController> logger)
        {
            _areaRepository = areaRepository;
            _storeRepository = storeRepository;
            _parkingRepository = parkingRepository;
            _venueEventRepository = venueEventRepository;
            _logger = logger;
        }

        // ---------------------------------------------------------
        // DTO 定义 (放在 Controller 内部或单独文件均可，这里为了方便直接包含)
        // ---------------------------------------------------------
        public class AreaCreateDto
        {
            public int AreaId { get; set; }
            public int IsEmpty { get; set; } // 0 或 1
            public int? AreaSize { get; set; }

            [Required]
            public string Category { get; set; } // "RETAIL", "EVENT", "PARKING", "OTHER"

            // Retail 特有
            public string? RentStatus { get; set; }
            public double? BaseRent { get; set; }

            // Event 特有
            public int? Capacity { get; set; }
            public int? AreaFee { get; set; } // 原定义为 int?

            // Parking 特有
            public int? ParkingFee { get; set; } // 原定义为 int?

            // Other 特有
            public string? Type { get; set; }
        }

        public class AreaUpdateDto
        {
            public int? IsEmpty { get; set; }
            public int? AreaSize { get; set; }

            // Retail
            public string? RentStatus { get; set; }
            public double? BaseRent { get; set; }

            // Event
            public int? Capacity { get; set; }
            public int? AreaFee { get; set; }

            // Parking
            public int? ParkingFee { get; set; }

            // Other
            public string? Type { get; set; }
        }
        // ---------------------------------------------------------

        // 辅助方法：手动组装 Composite 组件
        private IAreaComponent CreateComponent(int areaId, string category)
        {
            return category.ToUpper() switch
            {
                "RETAIL" => new RetailLeaf(_areaRepository, _storeRepository, areaId),
                "PARKING" => new ParkingLeaf(_areaRepository, _parkingRepository, areaId),
                "EVENT" => new EventLeaf(_areaRepository, _venueEventRepository, areaId),
                "OTHER" => new OtherLeaf(_areaRepository, areaId),
                _ => throw new ArgumentException($"不支持的区域类型: {category}")
            };
        }

        // POST: api/Areas
        [HttpPost]
        public async Task<IActionResult> CreateArea([FromBody] AreaCreateDto dto)
        {
            // 1. 检查是否存在 (Repo)
            var existing = await _areaRepository.GetAreaByIdAsync(dto.AreaId);
            if (existing != null)
            {
                return BadRequest($"区域ID '{dto.AreaId}' 已存在。");
            }

            // 2. 插入基表
            var area = new Area
            {
                AREA_ID = dto.AreaId,
                ISEMPTY = dto.IsEmpty,
                AREA_SIZE = dto.AreaSize,
                CATEGORY = dto.Category.ToUpper()
            };

            // 第一阶段：基表存入
            try
            {
                await _areaRepository.AddAsync(area);
                await _areaRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"基表数据插入失败: {dto.AreaId}");
                return StatusCode(500, "服务器内部错误：基表创建失败");
            }

            // 第二阶段：使用组件处理子表
            try
            {
                var component = CreateComponent(dto.AreaId, dto.Category);

                // 构造配置包 (注意类型转换)
                // 原 DTO 中 ParkingFee 和 AreaFee 是 int?，而 Configuration 中 Price 是 double?
                // 显式转换 (double?) 可以解决类型不匹配警告
                var config = new AreaConfiguration
                {
                    IsEmpty = dto.IsEmpty,
                    AreaSize = dto.AreaSize,

                    // 智能映射价格字段
                    Price = dto.Category.ToUpper() switch
                    {
                        "RETAIL" => dto.BaseRent,                   // double? -> double?
                        "PARKING" => (double?)dto.ParkingFee,       // int? -> double?
                        "EVENT" => (double?)dto.AreaFee,            // int? -> double?
                        _ => null
                    },

                    Status = dto.RentStatus,
                    Capacity = dto.Capacity,
                    TypeDescription = dto.Type
                };

                // Leaf 内部会调用 Repository 的 Upsert 方法
                await component.UpdateInfoAsync(config);

                return Ok(new { message = "区域创建成功" });
            }
            catch (Exception ex)
            {
                // 异常补偿：回滚基表
                _logger.LogError(ex, $"子表创建失败，正在回滚基表数据: {dto.AreaId}");
                try
                {
                    _areaRepository.Remove(area);
                    await _areaRepository.SaveChangesAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "回滚失败！");
                }

                return StatusCode(500, "创建区域失败: " + ex.Message);
            }
        }

        // GET: api/Areas/ByCategory
        [HttpGet("ByCategory")]
        public async Task<IActionResult> GetAreas([FromQuery] string? category, [FromQuery] int? isEmpty)
        {
            // 使用 Repository 的条件查询功能
            // 假设 IAreaRepository 继承自 IRepository<Area> 且有 FindAsync 或类似功能
            // 这里使用 lambda 表达式筛选
            var areas = await _areaRepository.FindAsync(a =>
                (string.IsNullOrEmpty(category) || a.CATEGORY == category.ToUpper()) &&
                (!isEmpty.HasValue || a.ISEMPTY == isEmpty.Value)
            );

            var resultList = new List<object>();

            foreach (var area in areas)
            {
                var component = CreateComponent(area.AREA_ID, area.CATEGORY);
                var info = await component.GetDetailsAsync();

                if (info == null) continue;

                // 映射回前端需要的匿名对象结构
                resultList.Add(new
                {
                    AREA_ID = info.AreaId,
                    ISEMPTY = info.IsEmpty,
                    AREA_SIZE = info.AreaSize,
                    CATEGORY = info.Category,

                    // 多态字段还原
                    BaseRent = info.Category == "RETAIL" ? info.Price : null,
                    RentStatus = info.Category == "RETAIL" ? info.BusinessStatus : null,

                    AreaFee = info.Category == "EVENT" ? info.Price : null,
                    Capacity = info.Category == "EVENT" ? info.CapacityOrSpaces : null,

                    ParkingFee = info.Category == "PARKING" ? (int?)info.Price : null,

                    Type = info.Category == "OTHER" ? info.SubType : null
                });
            }

            return Ok(resultList);
        }

        // GET: api/Areas/ByID
        [HttpGet("ByID")]
        public async Task<IActionResult> GetAreasByID([FromQuery] int id)
        {
            var area = await _areaRepository.GetAreaByIdAsync(id);
            if (area == null) return NotFound();

            var component = CreateComponent(area.AREA_ID, area.CATEGORY);
            var info = await component.GetDetailsAsync();

            if (info == null) return NotFound();

            var result = new
            {
                AREA_ID = info.AreaId,
                ISEMPTY = info.IsEmpty,
                AREA_SIZE = info.AreaSize,
                CATEGORY = info.Category,
                BaseRent = info.Category == "RETAIL" ? info.Price : null,
                RentStatus = info.Category == "RETAIL" ? info.BusinessStatus : null,
                AreaFee = info.Category == "EVENT" ? info.Price : null,
                Capacity = info.Category == "EVENT" ? info.CapacityOrSpaces : null,
                ParkingFee = info.Category == "PARKING" ? (int?)info.Price : null,
                Type = info.Category == "OTHER" ? info.SubType : null
            };

            return Ok(result);
        }

        // PUT: api/Areas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArea(int id, [FromBody] AreaUpdateDto dto)
        {
            var area = await _areaRepository.GetAreaByIdAsync(id);
            if (area == null) return NotFound($"未找到ID为 '{id}' 的区域。");

            try
            {
                var component = CreateComponent(id, area.CATEGORY);

                var config = new AreaConfiguration
                {
                    IsEmpty = dto.IsEmpty,
                    AreaSize = dto.AreaSize,

                    // 映射价格
                    Price = area.CATEGORY.ToUpper() switch
                    {
                        "RETAIL" => dto.BaseRent,
                        "PARKING" => dto.ParkingFee.HasValue ? (double)dto.ParkingFee.Value : null,
                        "EVENT" => dto.AreaFee.HasValue ? (double)dto.AreaFee.Value : null,
                        _ => null
                    },

                    Status = dto.RentStatus,
                    Capacity = dto.Capacity,
                    TypeDescription = dto.Type
                };

                await component.UpdateInfoAsync(config);

                return Ok(new { message = "区域信息更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新区域 {id} 失败。");
                return StatusCode(500, "更新区域信息失败，服务器内部错误。");
            }
        }

        // DELETE: api/Areas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArea(int id)
        {
            var area = await _areaRepository.GetAreaByIdAsync(id);
            if (area == null) return NotFound();

            // 1. 构建组件
            var component = CreateComponent(id, area.CATEGORY);

            // 2. 校验删除条件 (Repo 负责检查依赖)
            var error = await component.ValidateDeleteConditionAsync();
            if (error != null)
            {
                return BadRequest(error);
            }

            // 3. 执行删除
            try
            {
                _areaRepository.Remove(area);
                await _areaRepository.SaveChangesAsync();
                return Ok(new { message = "区域删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除区域 {id} 失败。");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}