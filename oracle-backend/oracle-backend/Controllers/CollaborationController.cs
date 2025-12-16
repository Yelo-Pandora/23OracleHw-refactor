using Microsoft.AspNetCore.Mvc;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollaborationController : ControllerBase
    {
        // [Repository Pattern] 使用 Repository 接口进行数据访问
        private readonly ICollaborationRepository _collabRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ILogger<CollaborationController> _logger;

        // [Dependency Injection] 注入 Repository 和 Logger
        public CollaborationController(
            ICollaborationRepository collabRepo,
            IAccountRepository accountRepo,
            ILogger<CollaborationController> logger)
        {
            _collabRepo = collabRepo;
            _accountRepo = accountRepo;
            _logger = logger;
        }

        // 权限验证方法
        private async Task<ActionResult?> CheckPermission(string operatorAccountId, int requiredAuthority)
        {
            if (string.IsNullOrEmpty(operatorAccountId))
            {
                return BadRequest("操作员账号不能为空");
            }

            // [Repository Pattern] 检查账号是否存在
            var account = await _accountRepo.FindAccountByUsername(operatorAccountId);
            if (account == null)
            {
                return BadRequest("操作员账号不存在");
            }

            // 检查账号是否被封禁
            if (account.AUTHORITY == 5)
            {
                return BadRequest("账号已被封禁，无法执行操作");
            }

            // [Repository Pattern] 检查常驻权限
            bool hasPermission = await _accountRepo.CheckAuthority(operatorAccountId, requiredAuthority);

            // [Repository Pattern] 如果没有常驻权限，检查临时权限
            if (!hasPermission)
            {
                var tempAuthorities = await _accountRepo.FindTempAuthorities(operatorAccountId);
                hasPermission = tempAuthorities.Any(ta =>
                    ta.TEMP_AUTHORITY.HasValue &&
                    ta.TEMP_AUTHORITY.Value <= requiredAuthority);
            }

            if (!hasPermission)
            {
                return BadRequest($"权限不足，需要权限级别 {requiredAuthority} 或更高");
            }

            return null;
        }

        // 2.4.1 添加新合作方
        [HttpPost]
        public async Task<IActionResult> AddCollaboration(
            [FromQuery, Required] string operatorAccountId,
            [FromBody] CollaborationDto dto)
        {
            // 验证权限 - 需要数据库管理员权限(1)
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;

            try
            {
                // [Repository Pattern] 检查ID唯一性
                var exists = await _collabRepo.ExistsAsync(dto.CollaborationId);

                if (exists)
                    return BadRequest("合作方ID已存在");

                // 创建实体
                var collaboration = new Collaboration
                {
                    COLLABORATION_ID = dto.CollaborationId,
                    COLLABORATION_NAME = dto.CollaborationName,
                    CONTACTOR = dto.Contactor,
                    PHONE_NUMBER = dto.PhoneNumber,
                    EMAIL = dto.Email
                };

                // [Repository Pattern] 添加并保存
                await _collabRepo.AddAsync(collaboration);
                await _collabRepo.SaveChangesAsync();

                _logger.LogInformation($"操作员 {operatorAccountId} 添加了合作方: ID={dto.CollaborationId}");
                return Ok(new { message = "添加成功", id = collaboration.COLLABORATION_ID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 添加合作方时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // 2.4.2 合作方信息查询
        [HttpGet]
        public async Task<IActionResult> SearchCollaborations(
            [FromQuery, Required] string operatorAccountId,
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] string? contactor)
        {
            // 验证权限 - 需要数据库管理员权限(1)或部门经理权限(2)
            var permissionCheck = await CheckPermission(operatorAccountId, 2);
            if (permissionCheck != null) return permissionCheck;

            // [Repository Pattern] 获取所有数据
            IEnumerable<Collaboration> query = await _collabRepo.GetAllAsync();

            // 在内存中应用筛选条件
            if (id.HasValue)
                query = query.Where(c => c.COLLABORATION_ID == id);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(c => c.COLLABORATION_NAME.Contains(name));

            if (!string.IsNullOrEmpty(contactor))
                query = query.Where(c => c.CONTACTOR != null && c.CONTACTOR.Contains(contactor));

            return Ok(query.ToList());
        }

        // 2.4.3 修改合作方信息
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollaboration(
            int id,
            [FromQuery, Required] string operatorAccountId,
            [FromBody] CollaborationUpdateDto dto)
        {
            // 验证权限 - 需要数据库管理员权限(1)
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;

            try
            {
                // [Repository Pattern] 查找指定ID的合作方
                var collaboration = await _collabRepo.GetByIdAsync(id);

                if (collaboration == null)
                    return NotFound("合作方不存在");

                // [Repository Pattern] 检查活动状态冲突
                if (await _collabRepo.HasActiveEventsAsync(id))
                    return BadRequest("存在进行中的合作活动，无法修改");

                // 更新可修改字段
                if (!string.IsNullOrEmpty(dto.CollaborationName))
                    collaboration.COLLABORATION_NAME = dto.CollaborationName;

                if (!string.IsNullOrEmpty(dto.Contactor))
                    collaboration.CONTACTOR = dto.Contactor;

                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                    collaboration.PHONE_NUMBER = dto.PhoneNumber;

                if (!string.IsNullOrEmpty(dto.Email))
                    collaboration.EMAIL = dto.Email;

                // [Repository Pattern] 更新并保存
                _collabRepo.Update(collaboration);
                await _collabRepo.SaveChangesAsync();

                _logger.LogInformation($"操作员 {operatorAccountId} 修改了合作方信息: ID={id}");
                return Ok(new
                {
                    message = "更新成功",
                    id = collaboration.COLLABORATION_ID,
                    updatedFields = new
                    {
                        name = dto.CollaborationName,
                        contactor = dto.Contactor,
                        phone = dto.PhoneNumber,
                        email = dto.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 更新合作方时出错");
                return StatusCode(500, "内部服务器错误" + ex);
            }
        }

        // 2.4.4 合作方统计报表
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
            [FromQuery, Required] string operatorAccountId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? industry)
        {
            // 验证权限
            var permissionCheck = await CheckPermission(operatorAccountId, 2);
            if (permissionCheck != null) return permissionCheck;

            if (startDate >= endDate)
            {
                return BadRequest("开始日期必须早于结束日期");
            }
            if (endDate > DateTime.Now)
            {
                return BadRequest("结束日期不能晚于当前日期");
            }
            return StatusCode(501, "报表功能需要扩展 Repository 支持复杂查询");
        }

        // 2.4.5 合作方数据删除
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollaboration(
            int id,
            [FromQuery, Required] string operatorAccountId)
        {
            // 验证权限
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;

            try
            {
                // [Repository Pattern] 获取实体
                var collaboration = await _collabRepo.GetByIdAsync(id);
                if (collaboration == null)
                    return NotFound("合作方不存在");

                // [Repository Pattern] 检查活动状态冲突
                if (await _collabRepo.HasActiveEventsAsync(id))
                    return BadRequest("存在进行中的合作活动，无法删除");

                // [Repository Pattern] 删除并保存
                _collabRepo.Remove(collaboration);
                await _collabRepo.SaveChangesAsync();

                _logger.LogInformation($"操作员 {operatorAccountId} 删除了合作方: ID={id}");
                return Ok(new { message = "删除成功", id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 删除合作方时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // DTO 类 (保持不变)
        public class CollaborationDto
        {
            [Required(ErrorMessage = "合作方ID是必填项")]
            [Range(1, 99999999999, ErrorMessage = "合作方ID必须大于0，且不能超过11位")]
            public int CollaborationId { get; set; }

            [Required(ErrorMessage = "合作方名称是必填项")]
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string CollaborationName { get; set; }

            [StringLength(50, ErrorMessage = "联系人姓名长度不能超过50个字符")]
            public string Contactor { get; set; }

            [Phone(ErrorMessage = "无效的电话号码格式")]
            [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
            public string PhoneNumber { get; set; }

            [EmailAddress(ErrorMessage = "无效的电子邮件格式")]
            [StringLength(50, ErrorMessage = "电子邮件长度不能超过50个字符")]
            public string Email { get; set; }
        }

        public class CollaborationUpdateDto
        {
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string CollaborationName { get; set; }

            [StringLength(50, ErrorMessage = "联系人姓名长度不能超过50个字符")]
            public string Contactor { get; set; }

            [Phone(ErrorMessage = "无效的电话号码格式")]
            [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
            public string PhoneNumber { get; set; }

            [EmailAddress(ErrorMessage = "无效的电子邮件格式")]
            [StringLength(50, ErrorMessage = "电子邮件长度不能超过50个字符")]
            public string Email { get; set; }
        }
    }
}