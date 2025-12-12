// 外观模式：商店系统具体实现类
// StoreSystemFacade.cs

using Microsoft.Extensions.Logging;
using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using oracle_backend.patterns.Facade_Pattern.Interfaces;
using oracle_backend.Patterns.Factory.Interfaces;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.State.RetailArea;
using oracle_backend.Patterns.State.Store;
using static oracle_backend.Controllers.AreasController;
using static oracle_backend.Controllers.StoreController;

namespace oracle_backend.patterns.Facade_Pattern.Implementations
{
    public class StoreSystemFacade : IStoreSystemFacade
    {
        private readonly IStoreRepository _storeRepo;
        private readonly IAreaRepository _areaRepository;
        private readonly IAccountRepository _accountRepo;
        private readonly IAreaComponentFactory _areaFactory;
        private readonly ILogger<StoreSystemFacade> _logger;

        // 模拟 Controller 中的静态字典逻辑
        private class ApplicationRecord
        {
            public string ApplicationNo { get; set; }
            public int StoreId { get; set; }
            public string ChangeType { get; set; }
            public string TargetStatus { get; set; }
            public string Reason { get; set; }
            public string Applicant { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "Pending";
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ApplicationRecord> _applications
            = new System.Collections.Concurrent.ConcurrentDictionary<string, ApplicationRecord>();

        public StoreSystemFacade(
            IStoreRepository storeRepo,
            IAreaRepository areaRepository,
            IAccountRepository accountRepo,
            IAreaComponentFactory areaFactory,
            ILogger<StoreSystemFacade> logger)
        {
            _storeRepo = storeRepo;
            _areaRepository = areaRepository;
            _accountRepo = accountRepo;
            _areaFactory = areaFactory;
            _logger = logger;
        }

        private StoreStateContext CreateStoreStateContext(Store store)
        {
            return new StoreStateContext(
                store.STORE_ID,
                store.STORE_STATUS,
                _logger
            );
        }

        private RetailAreaStateContext CreateRetailAreaStateContext(RetailArea retailArea)
        {
            return new RetailAreaStateContext(
                retailArea.AREA_ID,
                retailArea.BASE_RENT,
                retailArea.RENT_STATUS,
                _logger
            );
        }

        public async Task UpdateStoreStatusAsync(int storeId, string newStatus, string operatorAccount)
        {
            if (!string.IsNullOrEmpty(operatorAccount))
            {
                if (!await _accountRepo.CheckAuthority(operatorAccount, 2))
                    throw new UnauthorizedAccessException("权限不足");
            }

            var store = await _storeRepo.GetByIdAsync(storeId);
            if (store == null) throw new KeyNotFoundException("店铺不存在");

            // 使用状态模式验证状态转换
            var stateContext = CreateStoreStateContext(store);
            stateContext.TransitionToState(newStatus, "管理员更新状态");
            store.STORE_STATUS = stateContext.CurrentStateName;
            await _storeRepo.SaveChangesAsync();
        }

        public async Task<(bool Success, string Message, string ApplicationNo)> SubmitStatusChangeRequestAsync(StoreStatusChangeRequestDto dto)
        {
            var store = await _storeRepo.GetByIdAsync(dto.StoreId);
            if (store == null) return (false, "店面不存在", null);

            // 使用状态模式验证是否可以申请状态变更
            var stateContext = CreateStoreStateContext(store);
            if (!stateContext.CanRequestStatusChange(dto.TargetStatus))
            {
                return (false, $"不能从 {stateContext.CurrentStateName} 状态变更到 {dto.TargetStatus}", null);
            }

            // 通过状态模式请求状态变更
            var result = stateContext.RequestStatusChange(dto.TargetStatus, dto.Reason);

            if (!result.Success)
            {
                return (false, result.Message, null);
            }

            // 记录申请
            var appRecord = new ApplicationRecord
            {
                ApplicationNo = result.ApplicationNo,
                StoreId = dto.StoreId,
                ChangeType = dto.ChangeType,
                TargetStatus = dto.TargetStatus,
                Reason = dto.Reason,
                Applicant = dto.ApplicantAccount,
                CreatedAt = DateTime.Now
            };
            _applications[result.ApplicationNo] = appRecord;

            return (true, result.Message, result.ApplicationNo);
        }

        public async Task<(bool Success, string Message)> ApproveStatusChangeAsync(StoreStatusApprovalDto dto)
        {
            if (!await _accountRepo.CheckAuthority(dto.ApproverAccount, 2))
                return (false, "权限不足");

            if (!_applications.TryGetValue(dto.ApplicationNo, out var appRecord))
                return (false, "申请不存在");

            var store = await _storeRepo.GetByIdAsync(dto.StoreId);
            if (store == null) return (false, "店铺不存在");

            bool approved = dto.ApprovalAction == "通过";
            string targetStatus = string.IsNullOrEmpty(dto.TargetStatus) ? appRecord.TargetStatus : dto.TargetStatus;

            if (approved)
            {
                // 使用状态模式处理审批
                var stateContext = CreateStoreStateContext(store);
                stateContext.ApproveStatusChange(true, targetStatus);
                store.STORE_STATUS = stateContext.CurrentStateName;
                await _storeRepo.SaveChangesAsync();

                appRecord.Status = "Approved";
                return (true, "审批通过");
            }
            else
            {
                appRecord.Status = "Rejected";
                return (true, "审批驳回");
            }
        }

        public async Task UpdateAreaInfoAsync(int areaId, AreaUpdateDto dto)
        {
            var area = await _areaRepository.GetAreaByIdAsync(areaId);
            if (area == null) throw new KeyNotFoundException($"未找到ID为 '{areaId}' 的区域。");

            // [Factory Pattern] 使用工厂
            var component = _areaFactory.Create(areaId, area.CATEGORY);

            // [State Pattern] 如果是Retail区域且要更新租赁状态,使用状态模式验证
            if (area.CATEGORY.ToUpper() == "RETAIL" && !string.IsNullOrEmpty(dto.RentStatus))
            {
                var retailArea = await _areaRepository.GetRetailAreaDetailAsync(areaId);
                if (retailArea != null && dto.RentStatus != retailArea.RENT_STATUS)
                {
                    var stateContext = CreateRetailAreaStateContext(retailArea);

                    // 验证状态转换是否合法
                    if (!stateContext.CurrentState.CanTransitionTo(dto.RentStatus))
                    {
                        throw new InvalidOperationException($"不能从 {stateContext.CurrentStateName} 状态转换到 {dto.RentStatus}");
                    }

                    stateContext.TransitionToState(dto.RentStatus, "手动更新租赁状态");
                }
            }

            var config = new AreaConfiguration
            {
                IsEmpty = dto.IsEmpty,
                AreaSize = dto.AreaSize,
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
        }
    }
}
