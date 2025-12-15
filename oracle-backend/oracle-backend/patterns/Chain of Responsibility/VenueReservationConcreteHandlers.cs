using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;

namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 1. 时间有效性校验处理者
    /// 检查结束时间是否晚于起始时间
    /// </summary>
    public class TimeValidationHandler : VenueReservationHandler
    {
        public override async Task HandleAsync(VenueReservationRequest request)
        {
            // 验证时间有效性
            if (request.RentEndTime <= request.RentStartTime)
            {
                throw new VenueReservationException(
                    VenueReservationErrorCode.InvalidTimeRange,
                    "结束时间需晚于起始时间");
            }

            // 继续执行下一个处理者
            await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 2. 活动区域存在性校验处理者
    /// 检查活动区域是否存在
    /// </summary>
    public class EventAreaExistenceHandler : VenueReservationHandler
    {
        private readonly ComplexDbContext _context;

        public EventAreaExistenceHandler(ComplexDbContext context)
        {
            _context = context;
        }

        public override async Task HandleAsync(VenueReservationRequest request)
        {
            // 验证活动区域ID是否有效
            var eventArea = await _context.EventAreas
                .FirstOrDefaultAsync(ea => ea.AREA_ID == request.AreaId);

            if (eventArea == null)
            {
                throw new VenueReservationException(
                    VenueReservationErrorCode.EventAreaNotFound,
                    "活动区域ID无效");
            }

            // 将活动区域信息存储到请求对象中，供后续处理者使用
            request.EventArea = eventArea;

            // 继续执行下一个处理者
            await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 3. 区域占用情况校验处理者
    /// 检查时间段内区域是否已被占用
    /// </summary>
    public class AreaOccupancyHandler : VenueReservationHandler
    {
        private readonly ComplexDbContext _context;

        public AreaOccupancyHandler(ComplexDbContext context)
        {
            _context = context;
        }

        public override async Task HandleAsync(VenueReservationRequest request)
        {
            // 检查时间段内是否已被占用
            var conflictingReservation = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => 
                    ved.AREA_ID == request.AreaId &&
                    ((ved.RENT_START < request.RentEndTime && ved.RENT_END > request.RentStartTime)) &&
                    ved.STATUS != "已取消");

            if (conflictingReservation != null)
            {
                throw new VenueReservationException(
                    VenueReservationErrorCode.AreaAlreadyOccupied,
                    "该区域在指定时间内已被占用");
            }

            // 继续执行下一个处理者
            await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 4. 合作方存在性校验处理者
    /// 检查合作方是否存在
    /// </summary>
    public class CollaborationExistenceHandler : VenueReservationHandler
    {
        private readonly ComplexDbContext _context;

        public CollaborationExistenceHandler(ComplexDbContext context)
        {
            _context = context;
        }

        public override async Task HandleAsync(VenueReservationRequest request)
        {
            // 验证合作方是否存在
            if (!await _context.Collaborations
                .AnyAsync(c => c.COLLABORATION_ID == request.CollaborationId))
            {
                throw new VenueReservationException(
                    VenueReservationErrorCode.CollaborationNotFound,
                    "合作方信息不存在");
            }

            // 继续执行下一个处理者
            await base.HandleAsync(request);
        }
    }

    /// <summary>
    /// 5. 容量校验处理者
    /// 检查区域容量是否足够
    /// </summary>
    public class CapacityValidationHandler : VenueReservationHandler
    {
        public override async Task HandleAsync(VenueReservationRequest request)
        {
            // 验证容量是否足够（如果提供了预期人数）
            if (request.ExpectedHeadcount.HasValue && request.EventArea != null)
            {
                var areaCapacity = request.EventArea.CAPACITY ?? 0;
                if (request.ExpectedHeadcount.Value > areaCapacity)
                {
                    throw new VenueReservationException(
                        VenueReservationErrorCode.InsufficientCapacity,
                        $"活动区域容量不足，当前区域容量为 {areaCapacity} 人");
                }
            }

            // 继续执行下一个处理者
            await base.HandleAsync(request);
        }
    }
}
