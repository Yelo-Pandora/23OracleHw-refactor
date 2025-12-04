using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Services;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Threading.Tasks;
using oracle_backend.patterns.Composite_Pattern.Component; // 引入组件接口
using oracle_backend.patterns.Composite_Pattern.Leaf;      // 引入叶子节点

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        //private readonly CollaborationDbContext _collabContext;
        //private readonly ComplexDbContext _eventContext;
        private readonly AccountDbContext _accountContext;//这个DbContext仍需保留，有一个函数用到

        private readonly ICollaborationRepository _collabRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IVenueEventRepository _eventRepo;

        private readonly ILogger<StaffController> _logger;
        private readonly ILogger<AccountController> _accountLogger;

        public StaffController(
            //CollaborationDbContext collabContext,
            //ComplexDbContext eventContext,
            AccountDbContext accountContext,
            ICollaborationRepository collabRepo,
            IAccountRepository accountRepo,
            IVenueEventRepository eventRepo,
            ILogger<StaffController> logger,
            ILogger<AccountController> accountLogger)
        {
            //_collabContext = collabContext;
            //_eventContext = eventContext;
            _accountContext = accountContext;
            _collabRepo = collabRepo;
            _accountRepo = accountRepo;
            _eventRepo = eventRepo;
            _logger = logger;
            _accountLogger = accountLogger;
            _accountRepo = accountRepo;
        }

        // =========================================================================
        // 辅助函数，创建 Leaf 节点
        // 作用：隐藏 Leaf 的创建细节，Controller 只需知道它得到了一个 IPersonComponent
        // =========================================================================
        private IPersonComponent CreateStaffComponent(int staffId)
        {
            return new StaffLeaf(_collabRepo, _accountRepo, _eventRepo, staffId);
        }

        // DTO:
        public class StaffDto
        {
            [Required(ErrorMessage = "员工姓名是必填项")]
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string STAFF_NAME { get; set; }

            [StringLength(10, ErrorMessage = "性别长度不能超过10个字符")]
            public string STAFF_SEX { get; set; }

            [Required(ErrorMessage = "员工部门是必填项")]
            [StringLength(50, ErrorMessage = "部门长度不能超过50个字符")]
            public string STAFF_APARTMENT { get; set; }

            [Required(ErrorMessage = "员工职位是必填项")]
            [StringLength(50, ErrorMessage = "职位长度不能超过50个字符")]
            public string STAFF_POSITION { get; set; }

            [Required(ErrorMessage = "员工薪资是必填项")]
            [Range(0, 9999999999.99, ErrorMessage = "薪资必须大于等于0，且不能超过10位整数和2位小数")]
            public double STAFF_SALARY { get; set; }
        }

        public class SalaryDto
        {
            [Range(0, 9999999999.99, ErrorMessage = "薪资必须大于等于0，且不能超过10位整数和2位小数")]
            public double BASE_SALARY { get; set; }

            [Range(0, 9999999999.99, ErrorMessage = "奖金必须大于等于0，且不能超过10位整数和2位小数")]
            public double BONUS { get; set; }

            [Range(0, 9999999999.99, ErrorMessage = "罚金必须大于等于0，且不能超过10位整数和2位小数")]
            public double FINE { get; set; }
        }

        public class TempAuthorityDto
        {
            [Required(ErrorMessage = "账号是必填项")]
            [StringLength(50, ErrorMessage = "账号长度不能超过50个字符")]
            public string account { get; set; }

            [Required(ErrorMessage = "活动ID是必填项")]
            [Range(1, int.MaxValue, ErrorMessage = "活动ID必须大于0")]
            public int eventId { get; set; }

            [Required(ErrorMessage = "临时权限级别是必填项")]
            [Range(1, 5, ErrorMessage = "临时权限级别必须在1到5之间")]
            public int tempAuthority { get; set; }
        }

        // 权限与临时权限检查
        private async Task<bool> CheckPermission(string operatorAccountId, int requiredAuthority)
        {
            // 检查账号是否存在
            var account = await _accountRepo.FindAccountByUsername(operatorAccountId);
            // 检查权限
            bool hasPermission = await _accountRepo.CheckAuthority(operatorAccountId, requiredAuthority);
            if (!hasPermission)
            {
                // 检查临时权限
                var tempPermission = await _accountRepo.FindTempAuthorities(operatorAccountId);
                hasPermission = tempPermission.Any(ta =>
                    ta.TEMP_AUTHORITY.HasValue &&
                    ta.TEMP_AUTHORITY.Value <= requiredAuthority);
            }
            if (!hasPermission)
            {
                return false;
            }
            return true;
        }

        // 目前权限等级(取temp authority与authority中更小值)
        private async Task<int> GetCurrentAuthorityLevel(string operatorAccountId)
        {
            var account = await _accountRepo.FindAccountByUsername(operatorAccountId);
            if (account == null)
            {
                return 0; // 无权限
            }

            var tempAuthority = await _accountRepo.FindTempAuthorities(operatorAccountId);
            var currentAuthority = account.AUTHORITY;

            if (tempAuthority.Any())
            {
                currentAuthority = Math.Min(currentAuthority, tempAuthority.Min(ta => ta.TEMP_AUTHORITY.Value));
            }

            return currentAuthority;
        }

        // 是否有权限修改员工信息
        private async Task<ActionResult?> CanModifyStaff(string operatorAccount, string apartment)
        {
            var isAdmin = await CheckPermission(operatorAccount, 1);
            var isManager = await CheckPermission(operatorAccount, 2);
            if (!isAdmin && !isManager)
            {
                return BadRequest("无权限");
            }
            else if (!isAdmin && isManager)
            {
                // 查看manager的部门
                Staff manager = await _collabRepo.FindStaffByAccountAsync(operatorAccount);
                if (manager == null)
                {
                    return BadRequest("无效的操作员账号");
                }
                // 部门和员工不同则无权操作
                if (manager.STAFF_APARTMENT != apartment)
                {
                    return BadRequest("无权操作该部门员工");
                }
            }
            return null;
        }

        //获取所有员工信息
        [HttpGet("AllStaffs")]
        public async Task<IActionResult> GetAllStaffs()
        {
            var staffs = await _collabRepo.GetAllStaffsAsync();
            return Ok(staffs);
        }

        // 获取所有SalarySlip
        [HttpGet("AllsalarySlip")]
        public async Task<IActionResult> GetAllsalarySlip()
        {
            var salarySlip = await _collabRepo.GetAllSalarySlipsAsync();
            return Ok(salarySlip);
        }

        // 获取所有MonthSalaryCost
        [HttpGet("AllMonthSalaryCost")]
        public async Task<IActionResult> GetAllMonthSalaryCost()
        {
            var monthSalaryCost = await _collabRepo.GetAllSalarySlipsAsync();
            return Ok(monthSalaryCost);
        }

        // 查看所有temp authorities
        [HttpGet("AllTempAuthorities")]
        public async Task<IActionResult> GetAllTempAuthorities()
        {
            var tempAuthorities = await _accountContext.TEMP_AUTHORITY.ToListAsync();
            if (tempAuthorities == null || tempAuthorities.Count == 0)
            {
                return NotFound("没有临时权限记录");
            }
            return Ok(tempAuthorities);
        }

        // 2.6.1 添加新员工
        [HttpPost("AddStaff")]
        public async Task<IActionResult> AddStaff(
            [FromQuery, Required] string operatorAccount,
            [FromBody] StaffDto dto)
        {
            // 权限检查
            var Permission = await CanModifyStaff(operatorAccount, dto.STAFF_APARTMENT);
            if (Permission != null)
            {
                return Permission;
            }
            try
            {
                // 自动生成员工ID
                var maxStaffId = await _collabRepo.GetMaxStaffIdAsync();
                var newStaffId = maxStaffId + 1;
                var staff = new Staff
                {
                    STAFF_ID = newStaffId,
                    STAFF_NAME = dto.STAFF_NAME,
                    STAFF_SEX = dto.STAFF_SEX,
                    STAFF_APARTMENT = dto.STAFF_APARTMENT,
                    STAFF_POSITION = dto.STAFF_POSITION,
                    STAFF_SALARY = dto.STAFF_SALARY
                };

                // 生成员工新账号（账号为字符串，建议用员工ID或姓名+ID）
                var accountStr = $"staff{newStaffId}";
                var accountDto = new AccountController.AccountRegisterDto
                {
                    ACCOUNT = accountStr,
                    USERNAME = dto.STAFF_NAME,
                    PASSWORD = "DefaultPassword",
                    IDENTITY = "员工"
                };
                var accountController = new AccountController(_accountRepo, _accountLogger);
                var registerResult = await accountController.Register(accountDto) as ObjectResult;
                _logger.LogInformation("账号注册完成");
                if (registerResult == null || registerResult.StatusCode != 200)
                {
                    return BadRequest("创建员工账号失败");
                }

                // 添加员工
                await _collabRepo.AddStaffAsync(staff);
                await _collabRepo.SaveChangesAsync();

                // 建立员工与账号关联
                var staffAccount = new StaffAccount
                {
                    STAFF_ID = newStaffId,
                    ACCOUNT = accountStr
                };
                await _accountRepo.AddStaffAccountLink(staffAccount);
                await _accountRepo.SaveChangesAsync();
                _accountLogger.LogInformation($"员工与账号关联: ID={staff.STAFF_ID}, 账号={accountStr}");

                return Ok(new { message = "添加成功", id = staff.STAFF_ID, account = accountStr });
            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync();
                _logger.LogError(ex, "添加员工时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // 2.6.2 员工权限管理
        //[HttpPatch("ModifyStaffAuthority")]
        //public async Task<IActionResult> ModifyStaffAuthority(
        //    [FromQuery, Required] string operatorAccount,
        //    [FromQuery] int staffId,
        //    [FromQuery] int newAuthority)
        //{
        //    // 权限检查
        //    // 检查被修改的员工是否有临时权限
        //    var staffAccount = await _accountContext.AccountFromStaffID(staffId);
        //    if (staffAccount != null)
        //    {
        //        var tempAuthorities = await _accountContext.FindTempAuthorities(staffAccount.ACCOUNT);
        //        if (tempAuthorities != null && tempAuthorities.Count > 0)
        //        {
        //            return BadRequest("请先收回临时权限再调整长期权限");
        //        }
        //    }

        //    var staff = await _collabContext.FindStaffById(staffId);
        //    if (staff == null)
        //    {
        //        return BadRequest("员工不存在");
        //    }

        //    var Permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
        //    if (Permission != null) return Permission;

        //    // 操作员权限要高于被修改员工的权限(值越小权限越高)
        //    var currentAuthority = await GetCurrentAuthorityLevel(operatorAccount);
        //    var staffAuthority = await GetCurrentAuthorityLevel(staffAccount.ACCOUNT);
        //    if (currentAuthority >= staffAuthority)
        //    {
        //        return BadRequest("操作员权限要高于被修改员工的权限");
        //    }

        //    // newAuthority不可高于修改者的权限
        //    if (newAuthority < currentAuthority)
        //    {
        //        return BadRequest("不可分配高于自身的权限等级");
        //    }

        //    staffAccount.AUTHORITY = newAuthority;
        //    await _collabContext.SaveChangesAsync();
        //    await _accountContext.SaveChangesAsync();

        //    return Ok("员工权限修改成功");
        //}
        [HttpPatch("ModifyStaffAuthority")]
        public async Task<IActionResult> ModifyStaffAuthority(
            [FromQuery, Required] string operatorAccount,
            [FromQuery] int staffId,
            [FromQuery] int newAuthority)
        {
            // 1. 基础检查 (保持 Controller 的 HTTP 适配层职责)
            var staff = await _collabRepo.FindStaffByIdAsync(staffId);
            if (staff == null) return BadRequest("员工不存在");

            var Permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (Permission != null) return Permission;

            // 权限大小比较逻辑保留在 Controller，因为这是"操作者"是否有权发起请求的验证
            var currentAuthority = await GetCurrentAuthorityLevel(operatorAccount);
            var staffAccount = await _accountRepo.AccountFromStaffID(staffId);
            var staffAuthority = await GetCurrentAuthorityLevel(staffAccount.ACCOUNT);

            if (currentAuthority >= staffAuthority) return BadRequest("操作员权限要高于被修改员工的权限");
            if (newAuthority < currentAuthority) return BadRequest("不可分配高于自身的权限等级");

            try
            {
                // 2. [Composite 模式介入]
                // 构建组件
                IPersonComponent staffComponent = CreateStaffComponent(staffId);

                // 构建参数包
                var config = new AuthorityConfig
                {
                    PermanentAuthorityLevel = newAuthority
                    // EventId 为空，表示修改常驻权限
                };

                // 调用接口 (Leaf 内部会处理临时权限冲突检查和 DB 更新)
                await staffComponent.ManageAuthorityAsync(config);

                return Ok("员工权限修改成功");
            }
            catch (Exception ex)
            {
                // 捕获 Leaf 抛出的业务异常 (如"请先收回临时权限")
                return BadRequest(ex.Message);
            }
        }


        // 2.6.3 员工/管理员修改自己/下属的信息

        //[HttpPatch("ModifyStaffInfo")]
        //public async Task<IActionResult> UpdateStaff(
        //    [FromQuery, Required] int staffId,
        //    [FromQuery, Required] string operatorAccount,
        //    [FromBody] StaffDto dto)
        //{
        //    // 查找员工信息
        //    var staff = await _collabContext.FindStaffById(staffId);
        //    if (staff == null)
        //        return NotFound("员工不存在");

        //    // 判断操作员身份
        //    var operatorAccountObj = await _accountContext.FindAccount(operatorAccount);
        //    if (operatorAccountObj == null)
        //        return BadRequest("操作员账号不存在");

        //    // 判断是否为员工本人
        //    var staffAccount = await _accountContext.CheckStaff(operatorAccount);
        //    bool isSelf = staffAccount != null && staffAccount.STAFF_ID == staffId;

        //    // 管理员/管理人员权限
        //    var isAdmin = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);

        //    // 员工本人只能改姓名、性别
        //    if (isSelf && isAdmin != null)
        //    {
        //        // 只允许修改姓名和性别
        //        if (staff.STAFF_APARTMENT != dto.STAFF_APARTMENT ||
        //            staff.STAFF_POSITION != dto.STAFF_POSITION ||
        //            staff.STAFF_SALARY != dto.STAFF_SALARY)
        //        {
        //            return BadRequest("无权限修改，请联系管理人员");
        //        }

        //        staff.STAFF_NAME = dto.STAFF_NAME;
        //        staff.STAFF_SEX = dto.STAFF_SEX;
        //        await _collabContext.SaveChangesAsync();
        //        return Ok("修改成功");
        //    }

        //    // 管理人员或管理员可修改全部信息
        //    if (isAdmin == null)
        //    {
        //        staff.STAFF_NAME = dto.STAFF_NAME;
        //        staff.STAFF_SEX = dto.STAFF_SEX;
        //        staff.STAFF_APARTMENT = dto.STAFF_APARTMENT;
        //        staff.STAFF_POSITION = dto.STAFF_POSITION;
        //        staff.STAFF_SALARY = dto.STAFF_SALARY;
        //        await _collabContext.SaveChangesAsync();
        //        return Ok("修改成功");
        //    }

        //    return BadRequest("无权限修改");
        //}
        [HttpPatch("ModifyStaffInfo")]
        public async Task<IActionResult> UpdateStaff(
            [FromQuery, Required] int staffId,
            [FromQuery, Required] string operatorAccount,
            [FromBody] StaffDto dto)
        {
            var staff = await _collabRepo.FindStaffByIdAsync(staffId);
            if (staff == null) return NotFound("员工不存在");

            // 判断操作员身份
            var operatorAccountObj = await _accountRepo.FindAccountByUsername(operatorAccount);
            if (operatorAccountObj == null) return BadRequest("操作员账号不存在");

            var staffAccount = await _accountRepo.CheckStaff(operatorAccount);
            bool isSelf = staffAccount != null && staffAccount.STAFF_ID == staffId;
            var isAdmin = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);

            // 权限验证不通过
            if (!isSelf && isAdmin != null) return BadRequest("无权限修改");

            try
            {
                // 1. [Composite 模式介入] 构建配置对象
                var config = new StaffProfileConfig();

                if (isSelf && isAdmin != null)
                {
                    // 员工本人：只能改姓名、性别
                    // Leaf 的 UpdateProfileAsync 只会更新非 null 的字段
                    config.Name = dto.STAFF_NAME;
                    config.Sex = dto.STAFF_SEX;
                    // 其他字段保持 null，Leaf 不会去更新它们
                }
                else
                {
                    // 管理员：可修改全部信息
                    config.Name = dto.STAFF_NAME;
                    config.Sex = dto.STAFF_SEX;
                    config.Department = dto.STAFF_APARTMENT;
                    config.Position = dto.STAFF_POSITION;
                    config.BaseSalary = dto.STAFF_SALARY;
                }

                // 2. 调用组件
                IPersonComponent staffComponent = CreateStaffComponent(staffId);
                await staffComponent.UpdateProfileAsync(config);

                return Ok("修改成功");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        // 2.6.5 员工工资管理(底薪，奖金，罚金)
        //[HttpPost("StaffSalaryManagement")]
        //public async Task<IActionResult> ManageStaffSalary(
        //    [FromQuery, Required] string operatorAccount,
        //    [FromQuery, Required] int staffId,
        //    [FromQuery] DateTime monthTime, // 格式 如 2008-11
        //    [FromBody] SalaryDto dto)
        //{

        //    // 查询员工
        //    var staff = await _collabContext.FindStaffById(staffId);
        //    if (staff == null) return NotFound("员工不存在");

        //    // 权限检查
        //    var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
        //    if (permission != null) return permission;

        //    // 查询SalarySlip
        //    var salarySlip = await _collabContext.GetSalarySlipByStaffId(staffId, monthTime);
        //    if (salarySlip == null)
        //    {
        //        // 创建数据
        //        salarySlip = new SalarySlip
        //        {
        //            STAFF_ID = staffId,
        //            MONTH_TIME = monthTime,
        //            ATD_COUNT = 0,
        //            BONUS = dto.BONUS,
        //            FINE = dto.FINE
        //        };
        //        staff.STAFF_SALARY = dto.BASE_SALARY;
        //        await _collabContext.SalarySlips.AddAsync(salarySlip);
        //    }
        //    else
        //    {
        //        // 更新员工工资信息
        //        staff.STAFF_SALARY = dto.BASE_SALARY;
        //        salarySlip.BONUS = dto.BONUS;
        //        salarySlip.FINE = dto.FINE;
        //    }

        //    // 查询MonthSalaryCost
        //    var monthSalaryCost = await _collabContext.GetMonthSalaryCostByStaffId(monthTime);
        //    if (monthSalaryCost == null)
        //    {
        //        monthSalaryCost = new MonthSalaryCost
        //        {
        //            MONTH_TIME = monthTime,
        //            TOTAL_COST = (int)(staff.STAFF_SALARY + salarySlip.BONUS - salarySlip.FINE)
        //        };
        //        await _collabContext.MonthSalaryCosts.AddAsync(monthSalaryCost);
        //    }
        //    else
        //    {
        //        monthSalaryCost.TOTAL_COST = (int)(staff.STAFF_SALARY + salarySlip.BONUS - salarySlip.FINE);
        //    }
        //    await _collabContext.SaveChangesAsync();

        //    return Ok("员工工资信息修改成功");
        //}
        // =========================================================================
        // 修改点 3: 2.6.5 员工工资管理
        // 原逻辑：查询 SalarySlip, 计算 TotalCost, 更新 MonthSalaryCost 表
        // 新逻辑：这一整套复杂的关联表更新逻辑已全部封装在 Leaf.ManageSalaryAsync 中
        // =========================================================================
        [HttpPost("StaffSalaryManagement")]
        public async Task<IActionResult> ManageStaffSalary(
            [FromQuery, Required] string operatorAccount,
            [FromQuery, Required] int staffId,
            [FromQuery] DateTime monthTime,
            [FromBody] SalaryDto dto)
        {
            var staff = await _collabRepo.FindStaffByIdAsync(staffId);
            if (staff == null) return NotFound("员工不存在");

            var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (permission != null) return permission;

            try
            {
                // 1. [Composite 模式介入]
                IPersonComponent staffComponent = CreateStaffComponent(staffId);

                // 2. 构建参数包
                // 注意：StaffDto 中的 BASE_SALARY 对应 Config.NewBaseSalary
                var config = new SalaryManagementConfig
                {
                    MonthTime = monthTime,
                    NewBaseSalary = dto.BASE_SALARY,
                    Bonus = dto.BONUS,
                    Fine = dto.FINE
                };

                // 3. 调用接口
                // Leaf 会自动处理 SalarySlip 的创建/更新以及 MonthSalaryCost 的联动更新
                await staffComponent.ManageSalaryAsync(config);

                return Ok("员工工资信息修改成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "薪资管理失败");
                return StatusCode(500, "服务器内部错误");
            }
        }


        // 2.6.7 临时权限管理功能
        //[HttpPost("temporary_authority")]
        //public async Task<IActionResult> ManageTemporaryAuthority(
        //    [FromQuery, Required] string operatorAccount,
        //    [FromBody, Required] TempAuthorityDto dto)
        //{
        //    // 根据dto获取staff account
        //    var account = await _accountContext.FindAccount(dto.account);
        //    if (account == null) return NotFound("账号不存在");

        //    // 获取staff
        //    var staff = await _collabContext.FindStaffByAccount(dto.account);
        //    if (staff == null) return NotFound("员工不存在");

        //    // 权限检查
        //    var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
        //    if (permission != null) return permission;

        //    // 获取operator权限大小
        //    var operatorAuthority = await GetCurrentAuthorityLevel(operatorAccount);

        //    // 临时权限不得大于操作者权限
        //    if (dto.tempAuthority < operatorAuthority) return BadRequest("临时权限不得大于操作者权限");

        //    // 如果员工非临时权限大于等于该临时权限,则返回
        //    if (account.AUTHORITY <= dto.tempAuthority) return BadRequest("员工权限已大于等于该临时权限");

        //    // 如果员工权限大于操作者权限
        //    if (account.AUTHORITY < operatorAuthority) return BadRequest("该员工权限大于操作者权限");

        //    // 检查该活动是否存在, 若活动已结束，提示 “活动已结束”
        //    var saleEvent = await _eventContext.FindEventById(dto.eventId);
        //    if (saleEvent == null) return NotFound("活动不存在");
        //    //if (saleEvent.EVENT_END < DateTime.Now) return BadRequest("活动已结束");

        //    // 若已有该活动的权限
        //    var existingTempAuthority = await _accountContext.TEMP_AUTHORITY
        //        .FirstOrDefaultAsync(ta => ta.ACCOUNT == dto.account && ta.EVENT_ID == dto.eventId);
        //    if (existingTempAuthority != null)
        //    {
        //        // 如果临时权限大于操作者权限,则返回
        //        //if (existingTempAuthority.TEMP_AUTHORITY < operatorAuthority) return BadRequest("操作对象临时权限更大,不可修改");
        //        _accountContext.TEMP_AUTHORITY.Remove(existingTempAuthority);
        //    }

        //    // 创建临时权限
        //    var tempAuthority = new TempAuthority
        //    {
        //        ACCOUNT = dto.account,
        //        EVENT_ID = dto.eventId,
        //        TEMP_AUTHORITY = dto.tempAuthority,
        //    };
        //    await _accountContext.TEMP_AUTHORITY.AddAsync(tempAuthority);
        //    await _accountContext.SaveChangesAsync();

        //    return Ok("临时权限修改成功");
        //}
        [HttpPost("temporary_authority")]
        public async Task<IActionResult> ManageTemporaryAuthority(
            [FromQuery, Required] string operatorAccount,
            [FromBody, Required] TempAuthorityDto dto)
        {
            // 基础校验
            var account = await _accountRepo.FindAccountByUsername(dto.account);
            if (account == null) return NotFound("账号不存在");

            var staff = await _collabRepo.FindStaffByAccountAsync(dto.account);
            if (staff == null) return NotFound("员工不存在");

            var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (permission != null) return permission;

            // 权限等级校验 (Controller 层的业务规则)
            var operatorAuthority = await GetCurrentAuthorityLevel(operatorAccount);
            if (dto.tempAuthority < operatorAuthority) return BadRequest("临时权限不得大于操作者权限");
            if (account.AUTHORITY < operatorAuthority) return BadRequest("该员工权限大于操作者权限");

            try
            {
                // 1. [Composite 模式介入]
                IPersonComponent staffComponent = CreateStaffComponent(staff.STAFF_ID);

                // 2. 构建配置
                var config = new AuthorityConfig
                {
                    EventId = dto.eventId,
                    TempAuthorityLevel = dto.tempAuthority,
                    IsRevokeTemp = false // 授予模式
                };

                // 3. 调用接口
                // Leaf 内部会检查：活动是否存在、员工常驻权限是否已覆盖临时权限
                await staffComponent.ManageAuthorityAsync(config);

                return Ok("临时权限修改成功");
            }
            catch (Exception ex)
            {
                // Leaf 抛出的业务异常直接返回给前端
                return BadRequest(ex.Message);
            }
        }


        // 撤销临时权限
        //[HttpDelete("revoke_temporary_authority")]
        //public async Task<IActionResult> RevokeTemporaryAuthority(
        //    [FromQuery, Required] string operatorAccount,
        //    [FromQuery, Required] string staffAccount,
        //    [FromQuery, Required] int eventId)
        //{
        //    // 根据staffAccount获取staff
        //    var staff = await _collabContext.FindStaffByAccount(staffAccount);
        //    if (staff == null) return NotFound("员工不存在");

        //    // 权限检查
        //    var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
        //    if (permission != null) return permission;

        //    // 检查该活动是否存在, 若活动已结束，提示 “活动已结束”
        //    var saleEvent = await _eventContext.FindEventById(eventId);
        //    if (saleEvent == null) return NotFound("活动不存在");
        //    //if (saleEvent.EVENT_END < DateTime.Now) return BadRequest("活动已结束");

        //    // 撤销临时权限
        //    var tempAuthority = await _accountContext.TEMP_AUTHORITY
        //        .FirstOrDefaultAsync(ta => ta.ACCOUNT == staffAccount && ta.EVENT_ID == eventId);
        //    if (tempAuthority == null) return NotFound("临时权限不存在");

        //    _accountContext.TEMP_AUTHORITY.Remove(tempAuthority);
        //    await _accountContext.SaveChangesAsync();

        //    return Ok("临时权限撤销成功");
        //}
        [HttpDelete("revoke_temporary_authority")]
        public async Task<IActionResult> RevokeTemporaryAuthority(
            [FromQuery, Required] string operatorAccount,
            [FromQuery, Required] string staffAccount,
            [FromQuery, Required] int eventId)
        {
            var staff = await _collabRepo.FindStaffByAccountAsync(staffAccount);
            if (staff == null) return NotFound("员工不存在");

            var permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (permission != null) return permission;

            try
            {
                // 1. [Composite 模式介入]
                IPersonComponent staffComponent = CreateStaffComponent(staff.STAFF_ID);

                // 2. 构建配置 (撤销模式)
                var config = new AuthorityConfig
                {
                    EventId = eventId,
                    IsRevokeTemp = true
                };

                // 3. 调用接口
                await staffComponent.ManageAuthorityAsync(config);

                return Ok("临时权限撤销成功");
            }
            catch (Exception ex)
            {
                // 如果 Leaf 发现没有权限可撤销，会抛出异常
                if (ex.Message == "临时权限不存在") return NotFound("临时权限不存在");
                return BadRequest(ex.Message);
            }
        }
    }
}