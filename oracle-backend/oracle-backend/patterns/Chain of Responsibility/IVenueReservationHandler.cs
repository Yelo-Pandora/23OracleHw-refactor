using System;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;

namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 场地预约请求模型
    /// </summary>
    public class VenueReservationRequest
    {
        /// <summary>
        /// 合作方ID
        /// </summary>
        public int CollaborationId { get; set; }

        /// <summary>
        /// 区域ID
        /// </summary>
        public int AreaId { get; set; }

        /// <summary>
        /// 租用开始时间
        /// </summary>
        public DateTime RentStartTime { get; set; }

        /// <summary>
        /// 租用结束时间
        /// </summary>
        public DateTime RentEndTime { get; set; }

        /// <summary>
        /// 租用目的
        /// </summary>
        public string? RentPurpose { get; set; }

        /// <summary>
        /// 合作方名称
        /// </summary>
        public string CollaborationName { get; set; }

        /// <summary>
        /// 工作人员职位
        /// </summary>
        public string StaffPosition { get; set; }

        /// <summary>
        /// 活动名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 预期人数
        /// </summary>
        public int? ExpectedHeadcount { get; set; }

        /// <summary>
        /// 预期费用
        /// </summary>
        public double? ExpectedFee { get; set; }

        /// <summary>
        /// 容量
        /// </summary>
        public int? Capacity { get; set; }

        /// <summary>
        /// 费用
        /// </summary>
        public int? Expense { get; set; }

        /// <summary>
        /// 活动区域信息（用于后续处理）
        /// </summary>
        public EventArea? EventArea { get; set; }
    }

    /// <summary>
    /// 场地预约错误码
    /// </summary>
    public enum VenueReservationErrorCode
    {
        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 时间无效
        /// </summary>
        InvalidTimeRange = 1,

        /// <summary>
        /// 活动区域不存在
        /// </summary>
        EventAreaNotFound = 2,

        /// <summary>
        /// 区域已被占用
        /// </summary>
        AreaAlreadyOccupied = 3,

        /// <summary>
        /// 合作方不存在
        /// </summary>
        CollaborationNotFound = 4,

        /// <summary>
        /// 容量不足
        /// </summary>
        InsufficientCapacity = 5
    }

    /// <summary>
    /// 场地预约异常
    /// </summary>
    public class VenueReservationException : Exception
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public VenueReservationErrorCode ErrorCode { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="message">错误消息</param>
        public VenueReservationException(VenueReservationErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// 场地预约抽象处理者
    /// </summary>
    public abstract class VenueReservationHandler
    {
        /// <summary>
        /// 下一个处理者
        /// </summary>
        protected VenueReservationHandler? _nextHandler;

        /// <summary>
        /// 获取或设置下一个处理者
        /// </summary>
        public VenueReservationHandler? NextHandler
        {
            get { return _nextHandler; }
            protected set { _nextHandler = value; }
        }

        /// <summary>
        /// 设置下一个处理者
        /// </summary>
        /// <param name="handler">下一个处理者</param>
        /// <returns>下一个处理者，用于链式调用</returns>
        public VenueReservationHandler SetNext(VenueReservationHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="request">场地预约请求</param>
        /// <returns>如果处理成功返回true，否则抛出异常</returns>
        public virtual async Task HandleAsync(VenueReservationRequest request)
        {
            if (_nextHandler != null)
            {
                await _nextHandler.HandleAsync(request);
            }
        }
    }
}