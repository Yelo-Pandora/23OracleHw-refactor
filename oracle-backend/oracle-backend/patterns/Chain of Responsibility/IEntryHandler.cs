namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 车辆入场请求模型
    /// </summary>
    public class VehicleEntryRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public int SpaceId { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        
        // 用于存储处理过程中的数据，避免重复查询
        public Models.ParkingSpace? ParkingSpace { get; set; }
        public Models.Car? ExistingCar { get; set; }
    }

    /// <summary>
    /// 车辆入场处理者抽象基类
    /// 责任链模式的核心抽象类
    /// 优化版本：使用异常处理机制，简化返回值
    /// </summary>
    public abstract class EntryHandler
    {
        /// <summary>
        /// 下一个处理者
        /// </summary>
        protected EntryHandler? _nextHandler;
        
        /// <summary>
        /// 获取或设置下一个处理者（用于构建器访问）
        /// </summary>
        public EntryHandler? NextHandler
        {
            get { return _nextHandler; }
            protected set { _nextHandler = value; }
        }

        /// <summary>
        /// 设置下一个处理者，支持链式调用
        /// </summary>
        /// <param name="handler">下一个处理者</param>
        /// <returns>返回下一个处理者，支持链式调用</returns>
        public EntryHandler SetNext(EntryHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        /// <summary>
        /// 处理车辆入场请求
        /// 优化：使用异常处理机制，校验失败时抛出异常，成功时返回 true
        /// </summary>
        /// <param name="request">车辆入场请求</param>
        /// <returns>处理成功返回 true</returns>
        /// <exception cref="VehicleEntryException">校验失败时抛出异常</exception>
        public virtual async Task<bool> HandleAsync(VehicleEntryRequest request)
        {
            // 如果当前处理者通过，则传递给下一个处理者
            if (_nextHandler != null)
            {
                return await _nextHandler.HandleAsync(request);
            }
            
            // 如果没有下一个处理者，返回成功
            return true;
        }
    }
}



