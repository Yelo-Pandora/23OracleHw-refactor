using Microsoft.AspNetCore.Mvc;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.Factory.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenueEventController : ControllerBase
    {
        private readonly IVenueEventRepository _venueRepo;
        private readonly IVenueEventFactory _venueFactory;
        private readonly ILogger<VenueEventController> _logger;

        // 构造函数注入 Repository
        public VenueEventController(
            IVenueEventRepository venueRepo,
            IVenueEventFactory venueFactory,
            ILogger<VenueEventController> logger)
        {
            _venueRepo = venueRepo;
            _venueFactory = venueFactory;
            _logger = logger;
        }

        // DTOs (保持不变)
        public class VenueEventReservationDto
        {
            [Required] public int CollaborationId { get; set; }
            [Required] public int AreaId { get; set; }
            [Required] public DateTime RentStartTime { get; set; }
            [Required] public DateTime RentEndTime { get; set; }
            public string? RentPurpose { get; set; }
            [Required] public string CollaborationName { get; set; }
            [Required] public string StaffPosition { get; set; }
            [Required] public string EventName { get; set; }
            public int? ExpectedHeadcount { get; set; }
            public double? ExpectedFee { get; set; }
            public int? Capacity { get; set; }
            public int? Expense { get; set; }
        }

        public class VenueEventUpdateDto
        {
            public string? EventName { get; set; }
            public int? Headcount { get; set; }
            public string? Description { get; set; }
            public string? Status { get; set; }
            public List<string>? ParticipantAccounts { get; set; }
        }

        public class VenueEventSettlementDto
        {
            [Required] public int EventId { get; set; }
            [Required] public double VenueFee { get; set; }
            public double? AdditionalServiceFee { get; set; }
            [Required] public string PaymentMethod { get; set; }
            public string? InvoiceInfo { get; set; }
        }

        public class VenueEventReportRequestDto
        {
            [Required] public DateTime StartDate { get; set; }
            [Required] public DateTime EndDate { get; set; }
            public string? ReportType { get; set; }
        }

        public class VenueEventReportDto
        {
            public int TotalEvents { get; set; }
            public double TotalRentHours { get; set; }
            public double TotalRevenue { get; set; }
            public double AverageOccupancy { get; set; }
            public List<PopularVenueDto> PopularVenues { get; set; } = new List<PopularVenueDto>();
            public List<VenueEventSummaryDto> EventDetails { get; set; } = new List<VenueEventSummaryDto>();
        }

        public class PopularVenueDto
        {
            public int AreaId { get; set; }
            public int EventCount { get; set; }
            public double TotalRevenue { get; set; }
        }

        public class VenueEventSummaryDto
        {
            public int EventId { get; set; }
            public string? EventName { get; set; }
            public int AreaId { get; set; }
            public DateTime RentStart { get; set; }
            public DateTime RentEnd { get; set; }
            public double RentHours { get; set; }
            public double VenueFee { get; set; }
            public int? ActualHeadcount { get; set; }
            public string Status { get; set; }
        }

        // 1. 场地预约功能
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] VenueEventReservationDto dto)
        {
            // 验证时间有效性
            if (dto.RentEndTime <= dto.RentStartTime)
            {
                return BadRequest(new { message = "结束时间需晚于起始时间" });
            }

            // 验证活动区域ID是否有效 (使用 Repository 辅助检查)
            var eventArea = await _venueRepo.GetEventAreaByIdAsync(dto.AreaId);
            if (eventArea == null)
            {
                return BadRequest(new { message = "活动区域ID无效" });
            }

            // 检查时间段内是否已被占用 (使用 Repository 封装的逻辑)
            var conflictingReservation = await _venueRepo.GetConflictingReservationAsync(
                dto.AreaId, dto.RentStartTime, dto.RentEndTime);

            if (conflictingReservation != null)
            {
                return BadRequest(new { message = "该区域在指定时间内已被占用" });
            }

            // 验证合作方是否存在 (使用 Repository 辅助检查)
            if (!await _venueRepo.CollaborationExistsAsync(dto.CollaborationId))
            {
                return BadRequest(new { message = "合作方信息不存在" });
            }

            // 这里涉及两个表的插入，BaseRepository 通常共享同一个 Context 实例，
            // 只要是在同一个 Scope 内，多次 SaveChangesAsync 是安全的。
            try
            {
                // [重构] 使用工厂创建聚合对象 (Event + Detail)
                // 状态 "待审批" 等逻辑被封装在工厂内
                var result = _venueFactory.CreateReservation(dto, eventArea.CAPACITY ?? 0);

                await _venueRepo.AddAsync(result.Event);
                await _venueRepo.SaveChangesAsync(); // 获取 ID

                // 创建场地活动详情记录
                var venueEventDetail = new VenueEventDetail
                {
                    EVENT_ID = result.Event.EVENT_ID,
                    AREA_ID = dto.AreaId,
                    COLLABORATION_ID = dto.CollaborationId,
                    RENT_START = dto.RentStartTime,
                    RENT_END = dto.RentEndTime,
                    STATUS = "待审批",
                    FUNDING = 0
                };

                await _venueRepo.AddVenueEventDetailAsync(venueEventDetail);
                await _venueRepo.SaveChangesAsync();

                return Ok(new
                {
                    message = "场地预约申请提交成功，等待审批",
                    eventId = result.Event.EVENT_ID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建场地预约失败");
                return StatusCode(500, new { message = "创建场地预约失败" });
            }
        }

        // 2. 场地预约审批
        [HttpPut("reservations/{eventId}/approve")]
        public async Task<IActionResult> ApproveReservation(int eventId, [FromBody] string? approvalNote)
        {
            // 获取详情 (包含状态信息)
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的预约记录" });
            }

            if (venueEventDetail.STATUS != "待审批")
            {
                return BadRequest(new { message = "该预约已处理，无法重复审批" });
            }

            try
            {
                venueEventDetail.STATUS = "已通过";
                // Update 操作在 EF Core 中如果是被追踪实体，直接 SaveChanges 即可，
                // 或者调用 Update 方法显式标记
                // BaseRepository.Update(venueEventDetail) 需要泛型匹配，这里是 Detail 对象，
                // 所以我们依赖 SaveChangesAsync 的 ChangeTracker，或者在 Repo 中加 UpdateDetail 方法。
                // 鉴于 AddVenueEventDetailAsync 存在，EF Context 会追踪它。
                await _venueRepo.SaveChangesAsync();

                return Ok(new { message = "预约审批通过" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"审批预约 {eventId} 失败");
                return StatusCode(500, new { message = "审批失败" });
            }
        }

        // 3. 场地预约拒绝
        [HttpPut("reservations/{eventId}/reject")]
        public async Task<IActionResult> RejectReservation(int eventId, [FromBody] string? rejectionReason)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) return NotFound(new { message = "找不到对应的预约记录" });
            if (venueEventDetail.STATUS != "待审批") return BadRequest(new { message = "该预约已处理，无法重复审批" });

            try
            {
                venueEventDetail.STATUS = "已驳回";
                await _venueRepo.SaveChangesAsync();
                return Ok(new { message = "预约已驳回" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"驳回预约 {eventId} 失败");
                return StatusCode(500, new { message = "驳回失败" });
            }
        }

        // 4. 场地活动管理功能
        [HttpPut("events/{eventId}")]
        public async Task<IActionResult> UpdateVenueEvent(int eventId, [FromBody] VenueEventUpdateDto dto)
        {
            // 获取主活动实体 (BaseRepository 方法)
            var venueEvent = await _venueRepo.GetByIdAsync(eventId);
            if (venueEvent == null) return NotFound(new { message = "找不到对应的活动记录" });

            // 获取活动详情实体 (VenueEventRepository 扩展方法)
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);
            if (venueEventDetail == null) return NotFound(new { message = "找不到对应的活动详情记录" });

            if (venueEventDetail.STATUS == "已结束")
            {
                return BadRequest(new { message = "活动已结束，不可修改或取消" });
            }

            try
            {
                // 更新字段
                if (!string.IsNullOrEmpty(dto.EventName)) venueEvent.EVENT_NAME = dto.EventName;
                if (dto.Headcount.HasValue) venueEvent.HEADCOUNT = dto.Headcount.Value;
                if (!string.IsNullOrEmpty(dto.Status)) venueEventDetail.STATUS = dto.Status;

                // 处理参与人员 (批量导入)
                if (dto.ParticipantAccounts != null && dto.ParticipantAccounts.Any())
                {
                    // 先清空现有 (使用 Repo 方法)
                    await _venueRepo.RemoveTempAuthoritiesByEventIdAsync(eventId);

                    // 添加新的
                    foreach (var account in dto.ParticipantAccounts)
                    {
                        var tempAuthority = _venueFactory.CreateTempAuthority(account, eventId, 3);
                        await _venueRepo.AddTempAuthorityAsync(tempAuthority);
                    }
                }

                // 显式调用 Update (对于 BaseRepo 管理的主实体)
                _venueRepo.Update(venueEvent);
                await _venueRepo.SaveChangesAsync();

                return Ok(new { message = "活动信息更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新活动 {eventId} 失败");
                return StatusCode(500, new { message = "更新活动失败" });
            }
        }

        // 5. 取消活动
        [HttpPut("events/{eventId}/cancel")]
        public async Task<IActionResult> CancelVenueEvent(int eventId)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) return NotFound(new { message = "找不到对应的活动记录" });
            if (venueEventDetail.STATUS == "已结束") return BadRequest(new { message = "活动已结束，不可修改或取消" });

            try
            {
                venueEventDetail.STATUS = "已取消";
                await _venueRepo.SaveChangesAsync();
                return Ok(new { message = "活动已取消，场地资源已释放" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取消活动 {eventId} 失败");
                return StatusCode(500, new { message = "取消活动失败" });
            }
        }

        // 6. 查询活动列表
        [HttpGet("events")]
        public async Task<IActionResult> GetVenueEvents([FromQuery] string? status, [FromQuery] int? areaId)
        {
            // 使用 Repo 获取带导航属性的数据
            var events = await _venueRepo.GetVenueEventsWithDetailsAsync(status, areaId);

            // DTO 转换
            var result = events.Select(ved => new
            {
                ved.EVENT_ID,
                EventName = ved.venueEventNavigation.EVENT_NAME,
                ved.AREA_ID,
                ved.COLLABORATION_ID,
                CollaborationName = ved.collaborationNavigation.COLLABORATION_NAME,
                ved.RENT_START,
                ved.RENT_END,
                ved.STATUS,
                Headcount = ved.venueEventNavigation.HEADCOUNT,
                Fee = ved.venueEventNavigation.FEE,
                Capacity = ved.venueEventNavigation.CAPACITY,
                AreaFee = ved.eventAreaNavigation.AREA_FEE
            }).ToList();

            return Ok(result);
        }

        // 7. 场地活动结算收费功能
        [HttpPost("events/{eventId}/settlement")]
        public async Task<IActionResult> CreateSettlement(int eventId, [FromBody] VenueEventSettlementDto dto)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null) return NotFound(new { message = "找不到对应的活动记录" });
            if (venueEventDetail.STATUS != "已结束") return BadRequest(new { message = "只有已结束的活动才能进行结算" });

            try
            {
                var rentHours = (venueEventDetail.RENT_END - venueEventDetail.RENT_START).TotalHours;
                var totalFee = dto.VenueFee + (dto.AdditionalServiceFee ?? 0);

                var settlementInfo = new
                {
                    EventId = eventId,
                    EventName = venueEventDetail.venueEventNavigation.EVENT_NAME,
                    AreaId = venueEventDetail.AREA_ID,
                    RentStart = venueEventDetail.RENT_START,
                    RentEnd = venueEventDetail.RENT_END,
                    RentHours = Math.Round(rentHours, 2),
                    VenueFee = dto.VenueFee,
                    AdditionalServiceFee = dto.AdditionalServiceFee ?? 0,
                    TotalFee = totalFee,
                    PaymentMethod = dto.PaymentMethod,
                    InvoiceInfo = dto.InvoiceInfo,
                    SettlementTime = DateTime.Now
                };

                venueEventDetail.STATUS = "已结算";
                venueEventDetail.FUNDING = totalFee;
                await _venueRepo.SaveChangesAsync();

                return Ok(new
                {
                    message = "结算单生成成功",
                    settlement = settlementInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建结算单 {eventId} 失败");
                return StatusCode(500, new { message = "创建结算单失败" });
            }
        }

        // 8. 场地活动统计报表功能
        [HttpGet("reports")]
        public async Task<IActionResult> GenerateReport([FromQuery] VenueEventReportRequestDto dto)
        {
            if (dto.EndDate <= dto.StartDate) return BadRequest(new { message = "结束时间需晚于起始时间" });

            try
            {
                // 使用 Repo 获取范围内数据 (已 Include 导航属性)
                var events = await _venueRepo.GetVenueEventsInRangeAsync(dto.StartDate, dto.EndDate);
                // 转换为 List 以便进行后续的内存计算
                var eventsList = events.ToList();

                if (!eventsList.Any()) return Ok(new { message = "该时间段内无场地活动记录" });

                // 业务计算 (保持在 Controller)
                var totalEvents = eventsList.Count;
                var totalRentHours = eventsList.Sum(e => (e.RENT_END - e.RENT_START).TotalHours);
                var totalRevenue = eventsList.Sum(e => (e.eventAreaNavigation?.AREA_FEE ?? 0) *
                                                   (e.RENT_END - e.RENT_START).TotalHours);

                var eventsWithCapacity = eventsList.Where(e => e.venueEventNavigation.CAPACITY > 0).ToList();
                var averageOccupancy = eventsWithCapacity.Any() ?
                    eventsWithCapacity.Average(e => (double)(e.venueEventNavigation.HEADCOUNT ?? 0) / e.venueEventNavigation.CAPACITY * 100) : 0;

                var popularVenues = eventsList
                    .GroupBy(e => e.AREA_ID)
                    .Select(g => new PopularVenueDto
                    {
                        AreaId = g.Key,
                        EventCount = g.Count(),
                        TotalRevenue = g.Sum(e => (e.eventAreaNavigation?.AREA_FEE ?? 0) *
                                                  (e.RENT_END - e.RENT_START).TotalHours)
                    })
                    .OrderByDescending(p => p.EventCount)
                    .Take(10)
                    .ToList();

                var eventDetails = eventsList.Select(e => new VenueEventSummaryDto
                {
                    EventId = e.EVENT_ID,
                    EventName = e.venueEventNavigation.EVENT_NAME,
                    AreaId = e.AREA_ID,
                    RentStart = e.RENT_START,
                    RentEnd = e.RENT_END,
                    RentHours = Math.Round((e.RENT_END - e.RENT_START).TotalHours, 2),
                    VenueFee = (e.eventAreaNavigation?.AREA_FEE ?? 0) * (e.RENT_END - e.RENT_START).TotalHours,
                    ActualHeadcount = e.venueEventNavigation.HEADCOUNT,
                    Status = e.STATUS
                }).ToList();

                var report = new VenueEventReportDto
                {
                    TotalEvents = totalEvents,
                    TotalRentHours = Math.Round(totalRentHours, 2),
                    TotalRevenue = Math.Round(totalRevenue, 2),
                    AverageOccupancy = Math.Round(averageOccupancy, 2),
                    PopularVenues = popularVenues,
                    EventDetails = eventDetails
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成统计报表失败");
                return StatusCode(500, new { message = "生成统计报表失败" });
            }
        }

        // 9. 查询单个活动详情
        [HttpGet("events/{eventId}")]
        public async Task<IActionResult> GetVenueEventDetail(int eventId)
        {
            var venueEventDetail = await _venueRepo.GetEventDetailAsync(eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的活动记录" });
            }

            // 使用 Repo 获取参与人账号列表
            var participants = await _venueRepo.GetParticipantAccountsAsync(eventId);

            var result = new
            {
                venueEventDetail.EVENT_ID,
                EventName = venueEventDetail.venueEventNavigation.EVENT_NAME,
                venueEventDetail.AREA_ID,
                venueEventDetail.COLLABORATION_ID,
                CollaborationName = venueEventDetail.collaborationNavigation.COLLABORATION_NAME,
                venueEventDetail.RENT_START,
                venueEventDetail.RENT_END,
                venueEventDetail.STATUS,
                Headcount = venueEventDetail.venueEventNavigation.HEADCOUNT,
                Fee = venueEventDetail.venueEventNavigation.FEE,
                Capacity = venueEventDetail.venueEventNavigation.CAPACITY,
                Expense = venueEventDetail.venueEventNavigation.EXPENSE,
                AreaFee = venueEventDetail.eventAreaNavigation?.AREA_FEE,
                Participants = participants
            };

            return Ok(result);
        }
    }
}