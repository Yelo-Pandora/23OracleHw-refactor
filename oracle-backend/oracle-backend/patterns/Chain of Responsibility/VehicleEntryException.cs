namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 车辆入场错误码枚举
    /// 统一管理所有车辆入场相关的错误码
    /// </summary>
    public enum VehicleEntryErrorCode
    {
        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 车位不存在
        /// </summary>
        ParkingSpaceNotFound = 1001,
        /// <summary>
        /// 车辆已在场内
        /// </summary>
        VehicleAlreadyInside = 1002,
        /// <summary>
        /// 车位已被占用
        /// </summary>
        ParkingSpaceOccupied = 1003,
        /// <summary>
        /// 车辆在黑名单中
        /// </summary>
        VehicleBlacklisted = 1004,
        /// <summary>
        /// 用户余额不足
        /// </summary>
        InsufficientBalance = 1005
    }

    /// <summary>
    /// 车辆入场校验异常
    /// 用于责任链模式中的校验失败处理
    /// </summary>
    public class VehicleEntryException : Exception
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public VehicleEntryErrorCode ErrorCode { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="message">错误信息</param>
        public VehicleEntryException(VehicleEntryErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="message">错误信息</param>
        /// <param name="innerException">内部异常</param>
        public VehicleEntryException(VehicleEntryErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}

