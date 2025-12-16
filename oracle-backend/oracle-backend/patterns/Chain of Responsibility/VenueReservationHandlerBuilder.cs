using oracle_backend.Dbcontexts;

namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 场地预约责任链构建器
    /// 用于灵活配置责任链的处理者顺序和组合方式
    /// </summary>
    public class VenueReservationHandlerBuilder
    {
        private readonly ComplexDbContext _context;
        private VenueReservationHandler? _headHandler;
        private VenueReservationHandler? _tailHandler;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        public VenueReservationHandlerBuilder(ComplexDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 添加时间有效性校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddTimeValidationHandler()
        {
            AddHandler(new TimeValidationHandler());
            return this;
        }

        /// <summary>
        /// 添加活动区域存在性校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddEventAreaExistenceHandler()
        {
            AddHandler(new EventAreaExistenceHandler(_context));
            return this;
        }

        /// <summary>
        /// 添加区域占用情况校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddAreaOccupancyHandler()
        {
            AddHandler(new AreaOccupancyHandler(_context));
            return this;
        }

        /// <summary>
        /// 添加合作方存在性校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddCollaborationExistenceHandler()
        {
            AddHandler(new CollaborationExistenceHandler(_context));
            return this;
        }

        /// <summary>
        /// 添加容量校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddCapacityValidationHandler()
        {
            AddHandler(new CapacityValidationHandler());
            return this;
        }

        /// <summary>
        /// 添加自定义处理者
        /// </summary>
        /// <param name="handler">自定义处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder AddCustomHandler(VenueReservationHandler handler)
        {
            AddHandler(handler);
            return this;
        }

        /// <summary>
        /// 构建默认责任链
        /// 默认顺序：时间有效性校验 -> 活动区域存在性校验 -> 区域占用情况校验 -> 合作方存在性校验 -> 容量校验
        /// </summary>
        /// <returns>责任链的第一个处理者</returns>
        public VenueReservationHandler BuildDefaultChain()
        {
            return AddTimeValidationHandler()
                .AddEventAreaExistenceHandler()
                .AddAreaOccupancyHandler()
                .AddCollaborationExistenceHandler()
                .AddCapacityValidationHandler()
                .Build();
        }

        /// <summary>
        /// 构建责任链
        /// </summary>
        /// <returns>责任链的第一个处理者</returns>
        /// <exception cref="InvalidOperationException">当没有添加任何处理者时抛出</exception>
        public VenueReservationHandler Build()
        {
            if (_headHandler == null)
            {
                throw new InvalidOperationException("责任链中至少需要一个处理者");
            }
            return _headHandler;
        }

        /// <summary>
        /// 在特定类型的处理者之前插入处理者
        /// </summary>
        /// <typeparam name="T">目标处理者类型</typeparam>
        /// <param name="handler">要插入的处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder InsertHandlerBefore<T>(VenueReservationHandler handler) where T : VenueReservationHandler
        {
            if (_headHandler == null)
            {
                // 如果没有处理者，直接添加
                AddHandler(handler);
                return this;
            }

            // 特殊情况：要插入到头部
            if (_headHandler is T)
            {
                handler.SetNext(_headHandler);
                _headHandler = handler;
                return this;
            }

            // 一般情况：查找目标处理者的前一个处理者
            VenueReservationHandler current = _headHandler;
            while (current.NextHandler != null)
            {
                if (current.NextHandler is T)
                {
                    // 插入处理者
                    handler.SetNext(current.NextHandler);
                    current.SetNext(handler);
                    break;
                }
                current = current.NextHandler;
            }

            return this;
        }

        /// <summary>
        /// 在特定类型的处理者之后插入处理者
        /// </summary>
        /// <typeparam name="T">目标处理者类型</typeparam>
        /// <param name="handler">要插入的处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder InsertHandlerAfter<T>(VenueReservationHandler handler) where T : VenueReservationHandler
        {
            if (_headHandler == null)
            {
                AddHandler(handler);
                return this;
            }

            // 查找目标处理者
            VenueReservationHandler current = _headHandler;
            while (current != null)
            {
                if (current is T)
                {
                    // 插入处理者
                    handler.SetNext(current.NextHandler);
                    current.SetNext(handler);
                    // 如果插入的是新的尾处理者，更新尾处理者
                    if (handler.NextHandler == null)
                        _tailHandler = handler;
                    break;
                }
                current = current.NextHandler;
            }

            return this;
        }

        /// <summary>
        /// 替换特定类型的处理者
        /// </summary>
        /// <typeparam name="T">要替换的处理者类型</typeparam>
        /// <param name="newHandler">新的处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder ReplaceHandler<T>(VenueReservationHandler newHandler) where T : VenueReservationHandler
        {
            if (_headHandler == null)
                return this;

            // 特殊情况：要替换的是头处理者
            if (_headHandler is T)
            {
                newHandler.SetNext(_headHandler.NextHandler);
                _headHandler = newHandler;
                return this;
            }

            // 一般情况：查找要替换的处理者
            VenueReservationHandler current = _headHandler;
            while (current.NextHandler != null)
            {
                if (current.NextHandler is T)
                {
                    // 替换处理者
                    newHandler.SetNext(current.NextHandler.NextHandler);
                    current.SetNext(newHandler);
                    // 如果替换的是尾处理者，更新尾处理者
                    if (newHandler.NextHandler == null)
                        _tailHandler = newHandler;
                    break;
                }
                current = current.NextHandler;
            }

            return this;
        }

        /// <summary>
        /// 移除特定类型的处理者
        /// </summary>
        /// <typeparam name="T">要移除的处理者类型</typeparam>
        /// <returns>构建器实例，支持链式调用</returns>
        public VenueReservationHandlerBuilder RemoveHandler<T>() where T : VenueReservationHandler
        {
            if (_headHandler == null)
                return this;

            // 特殊情况：要移除的是头处理者
            if (_headHandler is T)
            {
                _headHandler = _headHandler.NextHandler;
                if (_headHandler == null)
                    _tailHandler = null;
                return this;
            }

            // 一般情况：查找要移除的处理者的前一个处理者
            VenueReservationHandler current = _headHandler;
            while (current.NextHandler != null)
            {
                if (current.NextHandler is T)
                {
                    // 移除处理者
                    current.SetNext(current.NextHandler.NextHandler);
                    // 如果移除的是尾处理者，更新尾处理者
                    if (current.NextHandler == null)
                        _tailHandler = current;
                    break;
                }
                current = current.NextHandler;
            }

            return this;
        }

        /// <summary>
        /// 内部方法：添加处理者到责任链
        /// </summary>
        /// <param name="handler">要添加的处理者</param>
        private void AddHandler(VenueReservationHandler handler)
        {
            if (_headHandler == null)
            {
                // 如果是第一个处理者，设置为头部和尾部
                _headHandler = handler;
                _tailHandler = handler;
            }
            else
            {
                // 否则，添加到尾部
                _tailHandler?.SetNext(handler);
                _tailHandler = handler;
            }
        }
    }
}
