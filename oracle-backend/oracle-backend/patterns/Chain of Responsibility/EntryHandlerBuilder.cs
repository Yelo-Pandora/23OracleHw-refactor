using oracle_backend.Dbcontexts;

namespace oracle_backend.patterns.Chain_of_Responsibility
{
    /// <summary>
    /// 车辆入场责任链构建器
    /// 用于灵活配置责任链的处理者顺序和组合方式
    /// </summary>
    public class EntryHandlerBuilder
    {
        private readonly ParkingContext _context;
        private EntryHandler? _headHandler;
        private EntryHandler? _tailHandler;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">停车库数据库上下文</param>
        public EntryHandlerBuilder(ParkingContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 添加车位存在性校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder AddSpaceExistenceHandler()
        {
            AddHandler(new SpaceExistenceHandler(_context));
            return this;
        }

        /// <summary>
        /// 添加重复车辆校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder AddDuplicateVehicleHandler()
        {
            AddHandler(new DuplicateVehicleHandler(_context));
            return this;
        }

        /// <summary>
        /// 添加车位状态校验处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder AddSpaceStatusHandler()
        {
            AddHandler(new SpaceStatusHandler(_context));
            return this;
        }





        /// <summary>
        /// 添加车辆入场执行处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder AddVehicleEntryExecutor()
        {
            AddHandler(new VehicleEntryExecutor(_context));
            return this;
        }

        /// <summary>
        /// 添加自定义处理者
        /// </summary>
        /// <param name="handler">自定义处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder AddCustomHandler(EntryHandler handler)
        {
            AddHandler(handler);
            return this;
        }

        /// <summary>
        /// 构建默认责任链
        /// 默认顺序：车位存在性校验 -> 重复车辆校验 -> 车位状态校验 -> 执行入场
        /// </summary>
        /// <returns>责任链的第一个处理者</returns>
        public EntryHandler BuildDefaultChain()
        {
            return AddSpaceExistenceHandler()
                .AddDuplicateVehicleHandler()
                .AddSpaceStatusHandler()
                .AddVehicleEntryExecutor()
                .Build();
        }

        /// <summary>
        /// 构建责任链
        /// </summary>
        /// <returns>责任链的第一个处理者</returns>
        /// <exception cref="InvalidOperationException">当没有添加任何处理者时抛出</exception>
        public EntryHandler Build()
        {
            if (_headHandler == null)
            {
                throw new InvalidOperationException("责任链中至少需要一个处理者");
            }
            return _headHandler;
        }

        /// <summary>
        /// 重置构建器，清空已添加的处理者
        /// </summary>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder Reset()
        {
            _headHandler = null;
            _tailHandler = null;
            return this;
        }

        /// <summary>
        /// 移除特定类型的处理者
        /// </summary>
        /// <typeparam name="T">要移除的处理者类型</typeparam>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder RemoveHandler<T>() where T : EntryHandler
        {
            if (_headHandler == null)
                return this;

            // 特殊情况：要移除的是头处理者
            if (_headHandler is T)
            {
                _headHandler = _headHandler.NextHandler;
                if (_headHandler == null) // 如果移除后没有处理者了
                    _tailHandler = null;
                return this;
            }

            // 一般情况：查找要移除的处理者的前一个处理者
            EntryHandler current = _headHandler;
            while (current.NextHandler != null)
            {
                if (current.NextHandler is T)
                {
                    // 移除处理者
                    // 这里需要修改EntryHandler类，添加SetNextHandler方法
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
        /// 在特定类型的处理者之前插入处理者
        /// </summary>
        /// <typeparam name="T">目标处理者类型</typeparam>
        /// <param name="handler">要插入的处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder InsertHandlerBefore<T>(EntryHandler handler) where T : EntryHandler
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
            EntryHandler current = _headHandler;
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
        /// 获取当前已注册的所有处理者
        /// </summary>
        /// <returns>处理者列表</returns>
        public List<EntryHandler> GetHandlers()
        {
            List<EntryHandler> handlers = new List<EntryHandler>();
            EntryHandler current = _headHandler;
            while (current != null)
            {
                handlers.Add(current);
                current = current.NextHandler;
            }
            return handlers;
        }

        /// <summary>
        /// 替换特定类型的处理者
        /// </summary>
        /// <typeparam name="T">要替换的处理者类型</typeparam>
        /// <param name="newHandler">新的处理者</param>
        /// <returns>构建器实例，支持链式调用</returns>
        public EntryHandlerBuilder ReplaceHandler<T>(EntryHandler newHandler) where T : EntryHandler
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
            EntryHandler current = _headHandler;
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
        /// 内部方法：添加处理者到责任链
        /// </summary>
        /// <param name="handler">要添加的处理者</param>
        private void AddHandler(EntryHandler handler)
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