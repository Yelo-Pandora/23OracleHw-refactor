using Microsoft.AspNetCore.Mvc;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/Accounts")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        // 只注入 IAccountRepository，保持与原 AccountDbContext 的一一对应关系
        private readonly IAccountRepository _accountRepo;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountRepository accountRepo, ILogger<AccountController> logger)
        {
            _accountRepo = accountRepo;
            _logger = logger;
        }

        // 获取所有账号信息
        [HttpGet("AllAccount")]
        public async Task<IActionResult> GetAllAccounts()
        {
            _logger.LogInformation("正在尝试获取所有账户信息...");
            try
            {
                var accounts = await _accountRepo.GetAllAsync();
                _logger.LogInformation($"成功获取到 {accounts.Count()} 个账户。");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取账户信息时发生错误。");
                return StatusCode(500, "服务器内部错误");
            }
        }

        public class AccountRegisterDto
        {
            [Required(ErrorMessage = "账号为必填项")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在3到50个字符之间")]
            public string ACCOUNT { get; set; }

            [Required(ErrorMessage = "密码为必填项")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度至少为6个字符")]
            public string PASSWORD { get; set; }

            [Required(ErrorMessage = "用户名为必填项")]
            [StringLength(50)]
            public string USERNAME { get; set; }

            [Required(ErrorMessage = "必须指定用户身份")]
            [StringLength(50)]
            public string IDENTITY { get; set; }
        }

        // 注册账号
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AccountRegisterDto registerDto)
        {
            _logger.LogInformation("正在尝试为 {AccountName} 注册新账号...", registerDto.ACCOUNT);

            try
            {
                // 使用 Repository 检查是否存在
                var existingAccount = await _accountRepo.FindAccountByUsername(registerDto.ACCOUNT);

                if (existingAccount != null)
                {
                    _logger.LogWarning("账号 {AccountName} 已存在，注册失败。", registerDto.ACCOUNT);
                    return Conflict("该账号已存在。");
                }

                int authorityCode;
                string identityValue = registerDto.IDENTITY;

                // 使用 Repository 获取总数
                bool isFirstUser = await _accountRepo.GetAccountCountAsync() == 0;

                if (isFirstUser)
                {
                    _logger.LogInformation("数据库中无用户，将 {AccountName} 设置为第一个管理员。", registerDto.ACCOUNT);
                    authorityCode = 1;
                    identityValue = "员工";
                }
                else
                {
                    switch (registerDto.IDENTITY)
                    {
                        case "员工": authorityCode = 3; break;
                        case "商户": authorityCode = 4; break;
                        default:
                            _logger.LogWarning("无效的身份类型 '{Identity}'，注册失败。", registerDto.IDENTITY);
                            return BadRequest($"无效的身份类型：'{registerDto.IDENTITY}'。有效值为：员工, 商户。");
                    }
                }

                var account = new Account
                {
                    ACCOUNT = registerDto.ACCOUNT,
                    USERNAME = registerDto.USERNAME,
                    PASSWORD = registerDto.PASSWORD,
                    IDENTITY = identityValue,
                    AUTHORITY = authorityCode
                };

                // 使用 Repository 添加并保存
                await _accountRepo.AddAsync(account);
                await _accountRepo.SaveChangesAsync();

                _logger.LogInformation("账号 {AccountName} 注册成功。", account.ACCOUNT);
                return Ok(new { message = "账号注册成功。" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行注册时发生内部错误。", registerDto.ACCOUNT);
                return StatusCode(500, "服务器内部错误，注册失败。");
            }
        }

        public class toLogin
        {
            public string acc { get; set; }
            public string pass { get; set; }
            public string identity { get; set; }
        }

        // 登陆账号
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] toLogin goal)
        {
            var target = await _accountRepo.FindAccountByUsername(goal.acc);
            if (target == null)
            {
                _logger.LogWarning("不存在该账号：{acc}", goal.acc);
                return BadRequest("用户不存在");
            }
            else if (target.PASSWORD != goal.pass)
            {
                _logger.LogWarning("密码不正确：{pass}", goal.pass);
                return BadRequest("密码不正确");
            }
            else if (target.AUTHORITY == 5)
            {
                _logger.LogWarning("账号已被封禁，禁止登录：{acc}", goal.acc);
                return BadRequest("账号封禁中");
            }
            else if (target.IDENTITY != goal.identity)
            {
                _logger.LogWarning("身份不正确或不存在该账号：{identity}", goal.identity);
                return BadRequest("身份错误");
            }
            else
                return Ok(target);
        }

        public class AccountUpdateDto
        {
            public string? USERNAME { get; set; }
            public string? PASSWORD { get; set; }
            public string? IDENTITY { get; set; }
            public int? AUTHORITY { get; set; }
            public string ACCOUNT { get; set; }
        }

        // 修改指定账号信息
        [HttpPatch("alter/{currAccount}")]
        public async Task<IActionResult> UpdateAccount(string currAccount, [FromQuery] string operatorAccountId, [FromBody] AccountUpdateDto updatedAccountDto)
        {
            var cur = await _accountRepo.FindAccountByUsername(currAccount);
            var oper = await _accountRepo.FindAccountByUsername(operatorAccountId);

            if (cur == null) return BadRequest("重新指定账号");
            if (oper == null) return BadRequest("请重试");

            if (oper.AUTHORITY != 1 && updatedAccountDto.ACCOUNT != null && currAccount.ToLower() != updatedAccountDto.ACCOUNT)
                return BadRequest("权限不足");

            if (updatedAccountDto.ACCOUNT != null && updatedAccountDto.ACCOUNT != currAccount)
                return BadRequest("不允许直接修改账号");

            if (updatedAccountDto.IDENTITY != null && updatedAccountDto.IDENTITY != "员工" && updatedAccountDto.IDENTITY != "商户")
                return BadRequest("无效的身份");

            // 更新实体字段
            if (updatedAccountDto.USERNAME != null) cur.USERNAME = updatedAccountDto.USERNAME;
            if (updatedAccountDto.IDENTITY != null) cur.IDENTITY = updatedAccountDto.IDENTITY;
            if (!string.IsNullOrEmpty(updatedAccountDto.PASSWORD))
            {
                cur.PASSWORD = updatedAccountDto.PASSWORD;
                _logger.LogInformation($"账号 {currAccount} 的密码已被更新。");
            }

            if (updatedAccountDto.AUTHORITY.HasValue && oper.AUTHORITY == 1 && currAccount != operatorAccountId)
            {
                cur.AUTHORITY = Math.Max(updatedAccountDto.AUTHORITY.Value, oper.AUTHORITY);
            }

            // 提交更改
            _accountRepo.Update(cur);
            await _accountRepo.SaveChangesAsync();
            return Ok("更新成功");
        }

        // 删除指定账号
        [HttpDelete("delete/{accountId}")]
        public async Task<IActionResult> DeleteAccount(string accountId, [FromQuery] string operatorAccountId)
        {
            var account = await _accountRepo.FindAccountByUsername(accountId);

            if (!await _accountRepo.CheckAuthority(operatorAccountId, 1))
            {
                _logger.LogWarning("非数据库管理员，无法删除");
                return BadRequest("非数据库管理员，无法删除");
            }

            if (account == null) return NotFound("账号不存在");

            try
            {
                // 1. 删除关联的员工账号
                var staffLink = await _accountRepo.CheckStaff(accountId);
                if (staffLink != null) _accountRepo.RemoveStaffAccountLink(staffLink);

                // 2. 删除关联的商家账号
                var storeLink = await _accountRepo.CheckStore(accountId);
                if (storeLink != null) _accountRepo.RemoveStoreAccountLink(storeLink);

                // 3. 删除关联的临时权限
                var tempAuthorities = await _accountRepo.FindTempAuthorities(accountId);
                if (tempAuthorities != null && tempAuthorities.Any())
                {
                    _accountRepo.RemoveTempAuthorities(tempAuthorities);
                }

                // 4. 删除主账号
                _accountRepo.Remove(account);

                // 5. 提交
                await _accountRepo.SaveChangesAsync();

                _logger.LogInformation($"账号 {accountId} 已由管理员 {operatorAccountId} 成功删除。");
                return Ok($"账号 '{accountId}' 已被成功删除。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除账号 {accountId} 时发生错误。");
                return StatusCode(500, "删除账号时发生数据库错误。");
            }
        }

        // 检查权限
        [HttpPost("chkauth")]
        public async Task<IActionResult> ChkAuthority(string account, int goalAuth)
        {
            var goalaccount = await _accountRepo.FindAccountByUsername(account);
            if (goalaccount == null) return BadRequest("账号不存在，请重新指定账号");
            return Ok(await _accountRepo.CheckAuthority(account, goalAuth));
        }

        // 查询权限
        [HttpPost("getauth")]
        public async Task<IActionResult> GetAuthority(string account)
        {
            var goalaccount = await _accountRepo.FindAccountByUsername(account);
            if (goalaccount == null) return BadRequest("账号不存在，请重新指定账号");
            return Ok(goalaccount.AUTHORITY);
        }

        // 查询账号信息
        [HttpGet("info/{accountId}")]
        public async Task<IActionResult> GetAccountInfo(string accountId)
        {
            _logger.LogInformation($"--- GetAccountInfo 被调用: [{accountId}] ---");
            var account = await _accountRepo.FindAccountByUsername(accountId);
            if (account == null) return NotFound("账号不存在");
            return Ok(account);
        }

        // 根据员工ID获取Account
        [HttpGet("GetAccById")]
        public async Task<IActionResult> GetAccountByStaffId(int staffId)
        {
            var account = await _accountRepo.AccountFromStaffID(staffId);
            if (account == null) return BadRequest("员工ID不存在，请重新指定");
            return Ok(account);
        }

        // 查询临时权限
        [HttpGet("tempauth/{accountId}")]
        public async Task<IActionResult> GetTempAuthorities(string accountId)
        {
            var account = await _accountRepo.FindAccountByUsername(accountId);
            if (account == null) return NotFound("账号不存在");

            var tempAuths = await _accountRepo.FindTempAuthorities(accountId);
            if (tempAuths == null) return Ok(new List<TempAuthority>());

            return Ok(tempAuths);
        }

        // DTOs
        public class StaffInfoDto
        {
            public int StaffId { get; set; }
            public string StaffName { get; set; }
            public string Department { get; set; }
            public string Position { get; set; }
            public string StaffSex { get; set; }
        }
        public class StoreInfoDto
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; }
            public string TenantName { get; set; }
        }
        public class AccountDetailDto
        {
            public string Account { get; set; }
            public string Username { get; set; }
            public string Identity { get; set; }
            public int Authority { get; set; }
            public StaffInfoDto? StaffInfo { get; set; }
            public StoreInfoDto? StoreInfo { get; set; }
        }

        // 查询所有账号详细信息
        [HttpGet("AllAccount/detailed")]
        public async Task<IActionResult> GetAllAccountDetails()
        {
            var allAccounts = await _accountRepo.GetAllAsync();
            if (allAccounts == null || !allAccounts.Any()) return Ok(new List<AccountDetailDto>());

            // 使用 Repository 新增的方法获取带导航属性的关联数据
            var staffLinks = await _accountRepo.GetAllStaffAccountsWithInfoAsync();
            var staffLinkDict = staffLinks.ToDictionary(sa => sa.ACCOUNT, sa => sa);

            var storeLinks = await _accountRepo.GetAllStoreAccountsWithInfoAsync();
            var storeLinkDict = storeLinks.ToDictionary(sa => sa.ACCOUNT, sa => sa);

            var responseDtos = new List<AccountDetailDto>();

            foreach (var accountEntity in allAccounts)
            {
                var detailDto = new AccountDetailDto
                {
                    Account = accountEntity.ACCOUNT,
                    Username = accountEntity.USERNAME,
                    Identity = accountEntity.IDENTITY,
                    Authority = accountEntity.AUTHORITY
                };

                if (staffLinkDict.TryGetValue(accountEntity.ACCOUNT, out var staffLink) && staffLink.staffNavigation != null)
                {
                    detailDto.StaffInfo = new StaffInfoDto
                    {
                        StaffId = staffLink.STAFF_ID,
                        StaffName = staffLink.staffNavigation.STAFF_NAME,
                        StaffSex = staffLink.staffNavigation.STAFF_SEX,
                        Department = staffLink.staffNavigation.STAFF_APARTMENT,
                        Position = staffLink.staffNavigation.STAFF_POSITION
                    };
                }

                if (storeLinkDict.TryGetValue(accountEntity.ACCOUNT, out var storeLink) && storeLink.storeNavigation != null)
                {
                    detailDto.StoreInfo = new StoreInfoDto
                    {
                        StoreId = storeLink.STORE_ID,
                        StoreName = storeLink.storeNavigation.STORE_NAME,
                        TenantName = storeLink.storeNavigation.TENANT_NAME
                    };
                }

                responseDtos.Add(detailDto);
            }

            return Ok(responseDtos);
        }

        // 单个账号详细信息
        [HttpGet("info/detailed/{accountId}")]
        public async Task<IActionResult> GetAccountDetail(string accountId)
        {
            var accountEntity = await _accountRepo.FindAccountByUsername(accountId);
            if (accountEntity == null) return NotFound("账号不存在");

            var detailDto = new AccountDetailDto
            {
                Account = accountEntity.ACCOUNT,
                Username = accountEntity.USERNAME,
                Identity = accountEntity.IDENTITY,
                Authority = accountEntity.AUTHORITY
            };

            // 手动获取关联信息，这里利用 CheckStaff/CheckStore 
            // 注意：如果 CheckStaff 返回的对象没有 Navigation，你需要确保 AccountRepo 的 CheckStaff 有 Include
            // 如果 AccountRepo.CheckStaff 只是调用 Context.CheckStaff (没有Include)，
            // 你可能需要在 Repo 里加一个 CheckStaffWithInfo，或者在这里接受只能拿到 ID

            // 为了简化，假设我们已经在 Repo 中处理了 Include，或者我们接受暂时只拿 ID
            // 根据上面的 AccountDbContext 代码，CheckStaff 返回的是 FirstOrDefaultAsync，通常不带 Include。
            // 但我们在 AccountRepository 中可以轻松在 CheckStaff 实现里加上 Include，或者使用 GetStaffByIdAsync

            var staffLink = await _accountRepo.CheckStaff(accountId);
            if (staffLink != null)
            {
                var staff = await _accountRepo.GetStaffByIdAsync(staffLink.STAFF_ID);
                if (staff != null)
                {
                    detailDto.StaffInfo = new StaffInfoDto
                    {
                        StaffId = staff.STAFF_ID,
                        StaffName = staff.STAFF_NAME,
                        StaffSex = staff.STAFF_SEX,
                        Department = staff.STAFF_APARTMENT,
                        Position = staff.STAFF_POSITION
                    };
                }
            }

            var storeLink = await _accountRepo.CheckStore(accountId);
            if (storeLink != null)
            {
                var store = await _accountRepo.GetStoreByIdAsync(storeLink.STORE_ID);
                if (store != null)
                {
                    detailDto.StoreInfo = new StoreInfoDto
                    {
                        StoreId = store.STORE_ID,
                        StoreName = store.STORE_NAME,
                        TenantName = store.TENANT_NAME
                    };
                }
            }
            return Ok(detailDto);
        }

        public class BindAccountDto
        {
            [Required(ErrorMessage = "账号为必填项")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在3到50个字符之间")]
            public string ACCOUNT { get; set; }
            [Required(ErrorMessage = "ID为必填项")]
            public int ID { get; set; }
            [Required(ErrorMessage = "类型为必填项")]
            [StringLength(10)]
            public string TYPE { get; set; }
        }

        // 绑定账号
        [HttpPost("bind")]
        public async Task<IActionResult> BindAccount([FromBody] BindAccountDto bindDto)
        {
            _logger.LogInformation("正在尝试绑定账号 {AccountName} 到 {Type} ID {Id}...", bindDto.ACCOUNT, bindDto.TYPE, bindDto.ID);
            try
            {
                var account = await _accountRepo.FindAccountByUsername(bindDto.ACCOUNT);
                if (account == null)
                {
                    return NotFound("该账号不存在。");
                }
                else if (bindDto.TYPE == "员工")
                {
                    // 使用 Repo 检查 Staff ID
                    var staff = await _accountRepo.GetStaffByIdAsync(bindDto.ID);
                    if (staff == null) return NotFound("该员工 ID 不存在。");

                    var existingStaffLink = await _accountRepo.CheckStaff(bindDto.ACCOUNT);
                    if (existingStaffLink != null) return Conflict("该账号已经绑定了一个员工。");

                    var existingStaffIdLink = await _accountRepo.AccountFromStaffID(bindDto.ID);
                    if (existingStaffIdLink != null) return Conflict("该员工已经绑定了一个账号。");

                    var staffAccount = new StaffAccount { ACCOUNT = bindDto.ACCOUNT, STAFF_ID = bindDto.ID };
                    await _accountRepo.AddStaffAccountLink(staffAccount);
                    await _accountRepo.SaveChangesAsync();
                    return Ok("绑定成功");
                }
                else if (bindDto.TYPE == "商户")
                {
                    // 使用 Repo 检查 Store ID
                    var store = await _accountRepo.GetStoreByIdAsync(bindDto.ID);
                    if (store != null)
                    {
                        var existingStoreLink = await _accountRepo.CheckStore(bindDto.ACCOUNT);
                        if (existingStoreLink != null) return Conflict("该账号已经绑定了一个商铺。");

                        var existingStoreIdLink = await _accountRepo.AccountFromStoreID(bindDto.ID);
                        if (existingStoreIdLink != null) return Conflict("该商铺已经绑定了一个账号。");

                        var storeAccount = new StoreAccount { ACCOUNT = bindDto.ACCOUNT, STORE_ID = bindDto.ID };
                        await _accountRepo.AddStoreAccountLink(storeAccount);
                        await _accountRepo.SaveChangesAsync();
                        return Ok("绑定成功");
                    }
                    else
                    {
                        return NotFound("该商铺 ID 不存在。");
                    }
                }
                else
                {
                    return BadRequest($"无效的绑定类型：'{bindDto.TYPE}'。有效值为：员工, 商户。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行绑定时发生内部错误。", bindDto.ACCOUNT);
                return StatusCode(500, "服务器内部错误，绑定失败。");
            }
        }

        // 解绑账号
        [HttpDelete("unbind")]
        public async Task<IActionResult> UnbindAccount([FromQuery] string account, [FromQuery] int ID, [FromQuery] string type)
        {
            _logger.LogInformation("正在尝试为账号 {AccountName} 与 {Type} ID {Id} 解绑...", account, type, ID);

            if (string.IsNullOrWhiteSpace(account) || ID <= 0 || string.IsNullOrWhiteSpace(type))
            {
                return BadRequest("必须提供有效的账号、ID 和类型。");
            }

            try
            {
                if (type == "员工")
                {
                    // 使用 Repo 获取特定的关联
                    var staffAccountLink = await _accountRepo.GetStaffAccountLink(account, ID);
                    if (staffAccountLink == null) return NotFound("未找到指定的员工绑定关系。");

                    _accountRepo.RemoveStaffAccountLink(staffAccountLink);
                    await _accountRepo.SaveChangesAsync();
                    return Ok("员工解绑成功");
                }
                else if (type == "商户")
                {
                    var storeAccountLink = await _accountRepo.GetStoreAccountLink(account, ID);
                    if (storeAccountLink == null) return NotFound("未找到指定的商户绑定关系。");

                    _accountRepo.RemoveStoreAccountLink(storeAccountLink);
                    await _accountRepo.SaveChangesAsync();
                    return Ok("商户解绑成功");
                }
                else
                {
                    return BadRequest($"无效的解绑类型：'{type}'。有效值为：员工, 商户。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行解绑时发生内部错误。", account);
                return StatusCode(500, "服务器内部错误，解绑失败。");
            }
        }

        // 获取活动列表
        [HttpGet("events")]
        public async Task<IActionResult> GetAllEvents()
        {
            try
            {
                // 使用 Repo 获取所有 Event
                var events = await _accountRepo.GetAllEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有活动列表时发生错误。");
                return StatusCode(500, "服务器内部错误，无法获取活动列表。");
            }
        }
    }
}