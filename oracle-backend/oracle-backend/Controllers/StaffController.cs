using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component; // 引入组件接口
using oracle_backend.patterns.Composite_Pattern.Leaf;      // 引入叶子节点
using oracle_backend.Patterns.Factory.Interfaces;          // 引用工厂接口
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Services;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Threading.Tasks;
using static oracle_backend.Controllers.AccountController; // 引用 DTO

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        //private readonly CollaborationDbContext _collabContext;
        //private readonly ComplexDbContext _eventContext;
        private readonly AccountDbContext _accountContext;//这个DbContext仍需保留，有一个函数用到

        // [Repository Pattern] 仓库接口
        private readonly ICollaborationRepository _collabRepo;
        // [Repository Pattern] 仓库接口
        private readonly IAccountRepository _accountRepo;

        // [Factory Pattern] 员工组件工厂接口 (用于创建 Composite Leaf)
        private readonly IPersonComponentFactory _personFactory;
        // [Factory Pattern] 账号工厂接口
        private readonly IAccountFactory _accountFactory;

        private readonly ILogger<StaffController> _logger;
        private readonly ILogger<AccountController> _accountLogger;

        public StaffController(
            //CollaborationDbContext collabContext,
            //ComplexDbContext eventContext,
            AccountDbContext accountContext,
            ICollaborationRepository collabRepo,
            IAccountRepository accountRepo,
            IPersonComponentFactory personFactory,
            IAccountFactory accountFactory,
            ILogger<StaffController> logger,
            ILogger<AccountController> accountLogger)
        {
            //_collabContext = collabContext;
            //_eventContext = eventContext;
            _accountContext = accountContext;
            _collabRepo = collabRepo;
            _accountRepo = accountRepo;
            _personFactory = personFactory;
            _accountFactory = accountFactory;
            _logger = logger;
            _accountLogger = accountLogger;
            _accountRepo = accountRepo;
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
                bool isFirstUser = await _accountRepo.GetAccountCountAsync() == 0;
                // [Factory Pattern] 创建 Account 实例
                var newAccount = _accountFactory.CreateAccount(accountDto, isFirstUser);

                await _accountRepo.AddAsync(newAccount);
                await _accountRepo.SaveChangesAsync();
                _logger.LogInformation("账号注册完成");

                // 添加员工
                await _collabRepo.AddStaffAsync(staff);
                await _collabRepo.SaveChangesAsync();

                // 建立员工与账号关联
                // [Factory Pattern] 创建 StaffAccountLink 实例
                var staffAccount = _accountFactory.CreateStaffLink(accountStr, newStaffId);
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
        [HttpPatch("ModifyStaffAuthority")]
        public async Task<IActionResult> ModifyStaffAuthority(
            [FromQuery, Required] string operatorAccount,
            [FromQuery, Required] int staffId,
            [FromQuery] int newAuthority)
        {
            // 基础检查
            var staff = await _collabRepo.FindStaffByIdAsync(staffId);
            if (staff == null) return BadRequest("员工不存在");

            var Permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (Permission != null) return Permission;

            var currentAuthority = await GetCurrentAuthorityLevel(operatorAccount);
            var staffAccount = await _accountRepo.AccountFromStaffID(staffId);
            var staffAuthority = await GetCurrentAuthorityLevel(staffAccount.ACCOUNT);

            if (currentAuthority >= staffAuthority) return BadRequest("操作员权限要高于被修改员工的权限");
            if (newAuthority < currentAuthority) return BadRequest("不可分配高于自身的权限等级");

            try
            {
                // [Factory Pattern] [Composite Pattern] 使用工厂创建组件
                IPersonComponent staffComponent = _personFactory.CreateStaff(staffId);

                var config = new AuthorityConfig
                {
                    PermanentAuthorityLevel = newAuthority
                };

                // [Composite Pattern] 调用组件接口
                await staffComponent.ManageAuthorityAsync(config);

                return Ok("员工权限修改成功");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // 2.6.3 员工/管理员修改自己/下属的信息
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
                // 1. [Composite Pattern] 构建配置对象
                var config = new StaffProfileConfig();

                if (isSelf && isAdmin != null)
                {
                    // 员工本人：只能改姓名、性别
                    config.Name = dto.STAFF_NAME;
                    config.Sex = dto.STAFF_SEX;
                    // Department, Position, BaseSalary 等字段保持 null, Leaf 不会更新
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

                // 2. [Factory Pattern] [Composite Pattern] 创建组件并调用
                IPersonComponent staffComponent = _personFactory.CreateStaff(staffId);
                // [Composite Pattern] 调用组件接口
                await staffComponent.UpdateProfileAsync(config);

                return Ok("修改成功");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        // 2.6.5 员工工资管理(底薪，奖金，罚金)
        // [Composite Pattern] 这一整套复杂的关联表更新逻辑已全部封装在 Leaf.ManageSalaryAsync 中
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
                // 1. [Factory Pattern] [Composite Pattern] 创建组件
                IPersonComponent staffComponent = _personFactory.CreateStaff(staffId);

                // 2. 构建参数包
                var config = new SalaryManagementConfig
                {
                    MonthTime = monthTime,
                    NewBaseSalary = dto.BASE_SALARY,
                    Bonus = dto.BONUS,
                    Fine = dto.FINE
                };

                // 3. [Composite Pattern] 调用接口
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
                // 1. [Factory Pattern] [Composite Pattern] 创建组件
                IPersonComponent staffComponent = _personFactory.CreateStaff(staff.STAFF_ID);

                // 2. 构建配置
                var config = new AuthorityConfig
                {
                    EventId = dto.eventId,
                    TempAuthorityLevel = dto.tempAuthority,
                    IsRevokeTemp = false // 授予模式
                };

                // 3. [Composite Pattern] 调用接口
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
                // [Factory Pattern] [Composite Pattern] 使用工厂创建组件
                IPersonComponent staffComponent = _personFactory.CreateStaff(staff.STAFF_ID);

                // 2. 构建配置 (撤销模式)
                var config = new AuthorityConfig
                {
                    EventId = eventId,
                    IsRevokeTemp = true
                };

                // 3. [Composite Pattern] 调用接口
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