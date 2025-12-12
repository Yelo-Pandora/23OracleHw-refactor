using Microsoft.AspNetCore.Mvc;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Patterns.Factory.Interfaces;
using oracle_backend.Patterns.State.Store;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace oracle_backend.Controllers
{
    // Refactored with State Pattern
    [Route("api/Store")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreRepository _storeRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IStoreFactory _storeFactory;
        // 保留 Context 用于尚未迁移的复杂业务逻辑
        private readonly StoreDbContext _legacyContext;
        private readonly ILogger<StoreController> _logger;

        public StoreController(
            IStoreRepository storeRepo,
            IAccountRepository accountRepo,
            IStoreFactory storeFactory,
            StoreDbContext legacyContext,
            ILogger<StoreController> logger)
        {
            _storeRepo = storeRepo;
            _accountRepo = accountRepo;
            _storeFactory = storeFactory;
            _legacyContext = legacyContext;
            _logger = logger;
        }

        /// <summary>
        /// 创建店铺状态上下文 (Refactored with State Pattern)
        /// </summary>
        private StoreStateContext CreateStoreStateContext(Store store)
        {
            return new StoreStateContext(
                store.STORE_ID,
                store.STORE_STATUS,
                _logger
            );
        }

        // ==========================================
        // 1. 基础 CRUD (使用 Repository)
        // ==========================================

        public class CreateRetailAreaDto
        {
            [Required] public int AreaId { get; set; }
            [Required][Range(1, int.MaxValue)] public int AreaSize { get; set; }
            [Required][Range(0.01, double.MaxValue)] public double BaseRent { get; set; }
            [Required] public string OperatorAccount { get; set; } = string.Empty;
        }

        [HttpPost("CreateRetailArea")]
        public async Task<IActionResult> CreateRetailArea([FromBody] CreateRetailAreaDto dto)
        {
            _logger.LogInformation("开始新增店面区域：{AreaId}", dto.AreaId);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                if (!await _accountRepo.CheckAuthority(dto.OperatorAccount, 1))
                    return BadRequest(new { error = "操作员权限不足，需要管理员权限" });

                if (await _storeRepo.AreaIdExists(dto.AreaId))
                    return BadRequest(new { error = "该区域ID已存在，请重新设置" });

                var retailArea = _storeFactory.CreateRetailArea(dto);

                // TPT 继承验证见文末
                await _storeRepo.AddRetailAreaAsync(retailArea);
                await _storeRepo.SaveChangesAsync();

                return Ok(new { message = "店面区域创建成功", areaId = dto.AreaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建店面区域失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        public class CreateMerchantDto
        {
            [Required] public string StoreName { get; set; }
            [Required] public string StoreType { get; set; }
            [Required] public string TenantName { get; set; }
            [Required] public string ContactInfo { get; set; }
            [Required] public int AreaId { get; set; }
            [Required] public DateTime RentStart { get; set; }
            [Required] public DateTime RentEnd { get; set; }
            public string? OperatorAccount { get; set; }
        }

        [HttpPost("CreateMerchant")]
        public async Task<IActionResult> CreateMerchant([FromBody] CreateMerchantDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                if (!await _accountRepo.CheckAuthority(dto.OperatorAccount, 1)) return BadRequest("权限不足");
                if (dto.RentEnd <= dto.RentStart) return BadRequest("时间错误");
                if (!await _storeRepo.IsAreaAvailable(dto.AreaId)) return BadRequest("店面已租用");
                if (await _storeRepo.TenantExists(dto.TenantName, dto.ContactInfo)) return BadRequest("租户已存在");

                // 1. 获取 ID (Repo 职责)
                var storeId = await _storeRepo.GetNextStoreId();

                // 2. [重构] 使用工厂创建所有相关实体 (Store, RentStore, Account, StoreAccount)
                //    这里封装了密码生成、默认状态、实体关联等逻辑
                var agg = _storeFactory.CreateMerchantAggregate(dto, storeId);

                // 3. 持久化 (Repo 职责)
                await _storeRepo.AddAsync(agg.Store);
                await _storeRepo.AddRentStore(agg.RentStore);
                await _accountRepo.AddAsync(agg.Account);
                await _accountRepo.AddStoreAccountLink(agg.StoreAccount);

                // 更新区域状态
                await _storeRepo.UpdateAreaStatus(dto.AreaId, false, "已租用");

                // 统一提交
                await _storeRepo.SaveChangesAsync();
                await _accountRepo.SaveChangesAsync();

                return Ok(new
                {
                    message = "商户创建成功",
                    storeId,
                    account = agg.Account.ACCOUNT,
                    initialPassword = agg.GeneratedPassword // 返回给前端
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建商户失败");
                return StatusCode(500, "服务器错误");
            }
        }

        [HttpPost("CreateMerchantByExistingAccount")]
        public async Task<IActionResult> CreateMerchantByExistingAccount([FromBody] CreateMerchantDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var operatorAcc = await _accountRepo.FindAccountByUsername(dto.OperatorAccount);
                if (operatorAcc == null) return BadRequest(new { error = "账号不存在" });

                if (dto.RentEnd <= dto.RentStart) return BadRequest(new { error = "时间错误" });
                if (!await _storeRepo.IsAreaAvailable(dto.AreaId)) return BadRequest(new { error = "店面非空置" });
                if (await _storeRepo.TenantExists(dto.TenantName, dto.ContactInfo)) return BadRequest(new { error = "租户已存在" });

                var storeId = await _storeRepo.GetNextStoreId();
                var result = _storeFactory.CreateMerchantWithExistingAccount(dto, storeId);

                await _storeRepo.AddAsync(result.Store);
                await _storeRepo.AddRentStore(result.RentStore);
                await _accountRepo.AddStoreAccountLink(result.Link);
                await _storeRepo.UpdateAreaStatus(dto.AreaId, false, "已租用");

                await _storeRepo.SaveChangesAsync();
                await _accountRepo.SaveChangesAsync();

                return Ok(new { message = "商户创建成功", storeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建商户失败");
                return StatusCode(500, "服务器错误");
            }
        }

        [HttpGet("AvailableAreas")]
        public async Task<IActionResult> GetAvailableAreas()
        {
            var areas = await _storeRepo.GetAvailableAreas();
            return Ok(areas);
        }

        [HttpGet("AllStores")]
        public async Task<IActionResult> GetAllStores([FromQuery] string? operatorAccount = null)
        {
            if (!string.IsNullOrEmpty(operatorAccount))
            {
                if (!await _accountRepo.CheckAuthority(operatorAccount, 2))
                    return BadRequest(new { error = "权限不足" });
            }
            var stores = await _storeRepo.GetAllAsync();
            return Ok(stores);
        }

        [HttpGet("{storeId}")]
        public async Task<IActionResult> GetStoreById(int storeId)
        {
            var store = await _storeRepo.GetByIdAsync(storeId);
            if (store == null) return NotFound();
            return Ok(store);
        }

        // Refactored with State Pattern - 使用状态模式更新店铺状态
        [HttpPatch("UpdateStoreStatus/{storeId}")]
        public async Task<IActionResult> UpdateStoreStatus(int storeId, [FromQuery] string newStatus, [FromQuery] string? operatorAccount = null)
        {
            if (!string.IsNullOrEmpty(operatorAccount))
            {
                if (!await _accountRepo.CheckAuthority(operatorAccount, 2))
                    return BadRequest(new { error = "权限不足" });
            }

            var store = await _storeRepo.GetByIdAsync(storeId);
            if (store == null) return NotFound();

            // 使用状态模式验证状态转换
            var stateContext = CreateStoreStateContext(store);
            
            try
            {
                stateContext.TransitionToState(newStatus, "管理员更新状态");
                store.STORE_STATUS = stateContext.CurrentStateName;
                await _storeRepo.SaveChangesAsync();
                return Ok(new { message = "更新成功" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==========================================
        // 2. 商户信息管理与审批 (部分复杂业务)
        // ==========================================

        public class UpdateMerchantInfoDto
        {
            [Required] public int StoreId { get; set; }
            public string? ContactInfo { get; set; }
            public string? Description { get; set; }
            public string? StoreType { get; set; }
            public DateTime? RentStart { get; set; }
            public DateTime? RentEnd { get; set; }
            public string? StoreStatus { get; set; }
            public string? StoreName { get; set; }
            [Required] public string OperatorAccount { get; set; }
        }

        [HttpPut("UpdateMerchantInfo")]
        public async Task<IActionResult> UpdateMerchantInfo([FromBody] UpdateMerchantInfoDto dto)
        {
            try
            {
                var operatorAcc = await _accountRepo.FindAccountByUsername(dto.OperatorAccount);
                if (operatorAcc == null) return BadRequest("账号不存在");

                bool isAdmin = operatorAcc.AUTHORITY <= 2;
                bool isMerchant = false;

                if (!isAdmin)
                {
                    // 使用 AccountRepo 新增的关联检查方法 (GetStoreAccountLink)
                    var storeAccount = await _accountRepo.GetStoreAccountLink(dto.OperatorAccount, dto.StoreId);
                    isMerchant = storeAccount != null;
                }

                if (!isAdmin && !isMerchant) return BadRequest(new { error = "无权限" });

                var store = await _storeRepo.GetByIdAsync(dto.StoreId);
                if (store == null) return NotFound();

                if (!string.IsNullOrEmpty(dto.ContactInfo)) store.CONTACT_INFO = dto.ContactInfo;
                if (isAdmin)
                {
                    if (dto.RentStart.HasValue) store.RENT_START = dto.RentStart.Value;
                    if (dto.RentEnd.HasValue) store.RENT_END = dto.RentEnd.Value;
                    if (!string.IsNullOrEmpty(dto.StoreType)) store.STORE_TYPE = dto.StoreType;
                    if (!string.IsNullOrEmpty(dto.StoreStatus)) store.STORE_STATUS = dto.StoreStatus;
                    if (!string.IsNullOrEmpty(dto.StoreName)) store.STORE_NAME = dto.StoreName;
                }

                await _storeRepo.SaveChangesAsync();
                return Ok(new { message = "更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 内存审批逻辑相关的 DTO 和 存储
        public class StoreStatusChangeRequestDto
        {
            public int StoreId { get; set; }
            public string ChangeType { get; set; }
            public string Reason { get; set; }
            public string TargetStatus { get; set; }
            public string ApplicantAccount { get; set; }
        }

        public class StoreStatusApprovalDto
        {
            public int StoreId { get; set; }
            public string ApprovalAction { get; set; }
            public string ApprovalComment { get; set; }
            public string ApproverAccount { get; set; }
            public string TargetStatus { get; set; }
            public string ApplicationNo { get; set; }
        }

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

        // Refactored with State Pattern - 使用状态模式提交状态变更申请
        [HttpPost("StatusChangeRequest")]
        public async Task<IActionResult> SubmitStatusChangeRequest([FromBody] StoreStatusChangeRequestDto dto)
        {
            var store = await _storeRepo.GetByIdAsync(dto.StoreId);
            if (store == null) return BadRequest("店面不存在");

            // 使用状态模式验证是否可以申请状态变更
            var stateContext = CreateStoreStateContext(store);
            
            if (!stateContext.CanRequestStatusChange(dto.TargetStatus))
            {
                return BadRequest(new { error = $"不能从 {stateContext.CurrentStateName} 状态变更到 {dto.TargetStatus}" });
            }

            // 通过状态模式请求状态变更
            var result = stateContext.RequestStatusChange(dto.TargetStatus, dto.Reason);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
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

            return Ok(new { message = result.Message, applicationNo = result.ApplicationNo });
        }

        // Refactored with State Pattern - 使用状态模式审批状态变更
        [HttpPost("ApproveStatusChange")]
        public async Task<IActionResult> ApproveStatusChange([FromBody] StoreStatusApprovalDto dto)
        {
            if (!await _accountRepo.CheckAuthority(dto.ApproverAccount, 2))
                return BadRequest("权限不足");

            if (!_applications.TryGetValue(dto.ApplicationNo, out var appRecord))
                return BadRequest("申请不存在");

            var store = await _storeRepo.GetByIdAsync(dto.StoreId);
            if (store == null)
                return NotFound("店铺不存在");

            bool approved = dto.ApprovalAction == "通过";
            string targetStatus = string.IsNullOrEmpty(dto.TargetStatus) ? appRecord.TargetStatus : dto.TargetStatus;

            if (approved)
            {
                // 使用状态模式处理审批
                var stateContext = CreateStoreStateContext(store);
                
                try
                {
                    stateContext.ApproveStatusChange(true, targetStatus);
                    store.STORE_STATUS = stateContext.CurrentStateName;
                    await _storeRepo.SaveChangesAsync();
                    
                    appRecord.Status = "Approved";
                    return Ok(new { message = "审批通过" });
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { error = ex.Message });
                }
            }
            else
            {
                appRecord.Status = "Rejected";
                return Ok(new { message = "审批驳回" });
            }
        }

        // ==========================================
        // 3. 复杂业务逻辑 (使用 _legacyContext)
        // ==========================================

        [HttpGet("BasicStatistics")]
        public async Task<IActionResult> GetBasicStatistics()
        {
            // 使用 Repo 中封装好的简单统计
            var stats = await _storeRepo.GetBasicStatistics();
            return Ok(stats);
        }

        [HttpPost("GenerateMonthlyRentBills")]
        public async Task<IActionResult> GenerateMonthlyRentBills([FromBody] string billPeriod)
        {
            // 使用 Context
            var bills = await _legacyContext.GenerateMonthlyRentBills(billPeriod);
            return Ok(new { message = "生成成功", bills });
        }

        [HttpPost("GetRentBills")]
        public async Task<IActionResult> GetRentBills([FromBody] RentBillQueryRequest request)
        {
            // 权限检查使用 Repo
            var op = await _accountRepo.FindAccountByUsername(request.OperatorAccount);
            if (op != null && op.AUTHORITY > 2)
            {
                var sa = await _accountRepo.GetStoreAccountByAccount(request.OperatorAccount);
                if (sa == null) return BadRequest("未关联店铺");
                request.StoreId = sa.STORE_ID;
            }

            // 查询使用 Context
            var bills = await _legacyContext.GetRentBillsDetails(request);
            return Ok(bills);
        }

        [HttpPost("PayRent")]
        public async Task<IActionResult> PayRent([FromBody] PayRentRequest request)
        {
            var success = await _legacyContext.ProcessRentPayment(request);
            if (!success) return BadRequest("支付失败");
            return Ok(new { message = "支付成功" });
        }

        [HttpPost("ConfirmPayment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            var success = await _legacyContext.ConfirmPayment(request);
            if (!success) return BadRequest("确认失败");
            return Ok(new { message = "确认成功" });
        }

        [HttpPost("UpdateOverdueStatus")]
        public async Task<IActionResult> UpdateOverdueStatus()
        {
            var count = await _legacyContext.UpdateOverdueStatus();
            return Ok(new { message = "更新成功", updatedCount = count });
        }

        // ==========================================
        // 4. 报表逻辑 (保留 Controller 内的私有方法，数据源改为 Context)
        // ==========================================

        [HttpGet("RentStatisticsReport")]
        public async Task<IActionResult> GetRentStatisticsReport([FromQuery] string startPeriod, [FromQuery] string endPeriod, [FromQuery] string dimension = "all", [FromQuery] string operatorAccount = "")
        {
            if (!string.IsNullOrEmpty(operatorAccount) && !await _accountRepo.CheckAuthority(operatorAccount, 2))
                return BadRequest("权限不足");

            var report = await GenerateRentStatisticsReport(startPeriod, dimension);
            return Ok(new { report });
        }

        // --- 私有报表生成方法 (内部调用 _legacyContext) ---

        private async Task<object> GenerateRentStatisticsReport(string period, string dimension)
        {
            switch (dimension.ToLower())
            {
                case "time": return await GenerateTimeBasedReport(period);
                case "area": return await GenerateRentAreaBasedReport(period);
                case "all":
                default: return await GenerateRentComprehensiveReport(period);
            }
        }

        private async Task<object> GenerateTimeBasedReport(string period)
        {
            // 使用 Context 获取数据
            var collectionStats = await _legacyContext.GetRentCollectionStatistics(period);
            var monthlyTrend = await _legacyContext.GetMonthlyTrend(period);

            return new
            {
                title = $"{period}年月租金收缴统计报表",
                summary = collectionStats,
                trend = monthlyTrend,
                insights = GenerateInsights(collectionStats)
            };
        }

        private async Task<object> GenerateRentAreaBasedReport(string period)
        {
            // 使用 Context 获取数据
            var areaStats = await _legacyContext.GetRentStatisticsByArea();

            return new
            {
                title = $"{period}年月租金收缴区域分析报表",
                areaDetails = areaStats.Select(a => new
                {
                    areaId = a.AreaId,
                    storeName = a.StoreName,
                    collectionStatus = GenerateAreaCollectionStatus(a.AreaId, period)
                }).ToList()
            };
        }

        private async Task<object> GenerateRentComprehensiveReport(string period)
        {
            dynamic timeReport = await GenerateTimeBasedReport(period);
            dynamic areaReport = await GenerateRentAreaBasedReport(period);

            return new
            {
                title = "综合报表",
                timeAnalysis = timeReport,
                areaAnalysis = areaReport
            };
        }

        // 纯逻辑辅助方法
        private string GenerateAreaCollectionStatus(int areaId, string period)
        {
            var random = new Random(areaId + int.Parse(period));
            return random.NextDouble() > 0.8 ? "逾期" : "已收缴";
        }

        private List<string> GenerateInsights(RentCollectionStatistics stats)
        {
            var insights = new List<string>();
            if (stats.CollectionRate >= 90) insights.Add("✅ 收缴率优秀");
            else insights.Add("⚠️ 收缴率需关注");
            return insights;
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}