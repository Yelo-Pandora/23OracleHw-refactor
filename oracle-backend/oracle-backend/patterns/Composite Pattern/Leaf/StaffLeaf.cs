//StaffLeaf.cs
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using System;
using System.Linq;
using System.Threading.Tasks;

//namespace oracle_backend.patterns.Composite_Pattern.Leaf
//{
//    /// <summary>
//    /// 员工叶子节点 (StaffLeaf)
//    /// 严格遵循 IPersonComponent 接口与 PersonDTOs 定义
//    /// </summary>
//    public class StaffLeaf : IPersonComponent
//    {
//        private readonly CollaborationDbContext _collabContext;
//        private readonly AccountDbContext _accountContext;
//        private readonly ComplexDbContext _eventContext;
//        private readonly int _staffId;

//        public StaffLeaf(
//            CollaborationDbContext collabContext,
//            AccountDbContext accountContext,
//            ComplexDbContext eventContext,
//            int staffId)
//        {
//            _collabContext = collabContext;
//            _accountContext = accountContext;
//            _eventContext = eventContext;
//            _staffId = staffId;
//        }

//        // =========================================================
//        // 1. 全维信息获取 (Read)
//        // 严格映射 PersonComponentInfo (Id, Name, Sex, Department, Position, Salary, Type)
//        // =========================================================
//        public async Task<PersonComponentInfo> GetDetailsAsync()
//        {
//            // 逻辑来源: StaffController.GetAllStaffs / FindStaffById
//            var staff = await _collabContext.FindStaffById(_staffId);
//            if (staff == null) return null;

//            return new PersonComponentInfo
//            {
//                Id = staff.STAFF_ID,
//                Name = staff.STAFF_NAME,
//                Sex = staff.STAFF_SEX,           // 对应 DTO 中的 Sex
//                Department = staff.STAFF_APARTMENT,
//                Position = staff.STAFF_POSITION,
//                Salary = staff.STAFF_SALARY,     // 对应 DTO 中的 Salary
//                Type = "Employee"
//            };
//        }

//        // =========================================================
//        // 2. 档案变更 (Write - Basic Info)
//        // 逻辑来源: StaffController.UpdateStaff
//        // =========================================================
//        public async Task UpdateProfileAsync(StaffProfileConfig config)
//        {
//            var staff = await _collabContext.FindStaffById(_staffId);
//            if (staff == null) throw new Exception("员工不存在");

//            // 根据 Config 动态更新字段，不为 null 才更新
//            if (config.Name != null) staff.STAFF_NAME = config.Name;
//            if (config.Sex != null) staff.STAFF_SEX = config.Sex;
//            if (config.Department != null) staff.STAFF_APARTMENT = config.Department;
//            if (config.Position != null) staff.STAFF_POSITION = config.Position;
//            if (config.BaseSalary.HasValue) staff.STAFF_SALARY = config.BaseSalary.Value;

//            await _collabContext.SaveChangesAsync();
//        }

//        // =========================================================
//        // 3. 薪资管理 (Write - Salary)
//        // 逻辑来源: StaffController.ManageStaffSalary
//        // =========================================================
//        public async Task ManageSalaryAsync(SalaryManagementConfig config)
//        {
//            var staff = await _collabContext.FindStaffById(_staffId);
//            if (staff == null) throw new Exception("员工不存在");

//            // 1. 更新员工基础薪资 (如果配置中包含)
//            if (config.NewBaseSalary.HasValue)
//            {
//                staff.STAFF_SALARY = config.NewBaseSalary.Value;
//            }

//            // 2. 查询或创建 SalarySlip (工资条)
//            var salarySlip = await _collabContext.GetSalarySlipByStaffId(_staffId, config.MonthTime);
//            if (salarySlip == null)
//            {
//                salarySlip = new SalarySlip
//                {
//                    STAFF_ID = _staffId,
//                    MONTH_TIME = config.MonthTime,
//                    ATD_COUNT = 0,
//                    BONUS = config.Bonus,
//                    FINE = config.Fine
//                };
//                await _collabContext.SalarySlips.AddAsync(salarySlip);
//            }
//            else
//            {
//                // 更新现有条目
//                salarySlip.BONUS = config.Bonus;
//                salarySlip.FINE = config.Fine;
//            }

//            // 3. 更新 MonthSalaryCost (每月工资总支出)
//            // 逻辑搬运: TOTAL_COST = STAFF_SALARY + BONUS - FINE
//            var monthSalaryCost = await _collabContext.GetMonthSalaryCostByStaffId(config.MonthTime);
//            int currentTotal = (int)(staff.STAFF_SALARY + salarySlip.BONUS - salarySlip.FINE);

//            if (monthSalaryCost == null)
//            {
//                monthSalaryCost = new MonthSalaryCost
//                {
//                    MONTH_TIME = config.MonthTime,
//                    TOTAL_COST = currentTotal
//                };
//                await _collabContext.MonthSalaryCosts.AddAsync(monthSalaryCost);
//            }
//            else
//            {
//                // 注意：Controller 原逻辑是用单人薪资覆盖了 TOTAL_COST，这里保持一致
//                // 如果实际业务是累加，Controller 逻辑可能需要修正，但在此处我严格照抄 Controller 实现
//                monthSalaryCost.TOTAL_COST = currentTotal;
//            }

//            await _collabContext.SaveChangesAsync();
//        }

//        // =========================================================
//        // 3.1 成本计算 (Read - Financial)
//        // 逻辑来源: StaffController.GetAllMonthSalaryCost (细化到单人)
//        // =========================================================
//        public async Task<double> CalculateMonthlyCostAsync(DateTime month)
//        {
//            var staff = await _collabContext.FindStaffById(_staffId);
//            if (staff == null) return 0;

//            var salarySlip = await _collabContext.GetSalarySlipByStaffId(_staffId, month);

//            double baseSalary = staff.STAFF_SALARY;
//            double bonus = salarySlip?.BONUS ?? 0;
//            double fine = salarySlip?.FINE ?? 0;

//            return baseSalary + bonus - fine;
//        }

//        // =========================================================
//        // 4. 权限控制 (Write - Authority)
//        // 逻辑来源: StaffController.ModifyStaffAuthority, ManageTemporaryAuthority, RevokeTemporaryAuthority
//        // =========================================================
//        public async Task ManageAuthorityAsync(AuthorityConfig config)
//        {
//            var staffAccount = await _accountContext.AccountFromStaffID(_staffId);
//            if (staffAccount == null) throw new Exception("该员工未绑定账号");

//            // A. 处理常驻权限 (ModifyStaffAuthority)
//            if (config.PermanentAuthorityLevel.HasValue)
//            {
//                // 逻辑搬运: 检查被修改的员工是否有临时权限
//                var tempAuthorities = await _accountContext.FindTempAuthorities(staffAccount.ACCOUNT);
//                if (tempAuthorities != null && tempAuthorities.Count > 0)
//                {
//                    throw new Exception("请先收回临时权限再调整长期权限");
//                }

//                // 修改权限
//                staffAccount.AUTHORITY = config.PermanentAuthorityLevel.Value;
//            }

//            // B. 处理临时权限
//            if (config.EventId.HasValue)
//            {
//                // 检查活动是否存在 (逻辑搬运)
//                var saleEvent = await _eventContext.FindEventById(config.EventId.Value);
//                if (saleEvent == null) throw new Exception("活动不存在");

//                var existingTempAuth = await _accountContext.TEMP_AUTHORITY
//                    .FirstOrDefaultAsync(ta => ta.ACCOUNT == staffAccount.ACCOUNT && ta.EVENT_ID == config.EventId.Value);

//                // B1. 撤销临时权限 (RevokeTemporaryAuthority)
//                if (config.IsRevokeTemp)
//                {
//                    if (existingTempAuth == null) throw new Exception("临时权限不存在");
//                    _accountContext.TEMP_AUTHORITY.Remove(existingTempAuth);
//                }
//                // B2. 授予/修改临时权限 (ManageTemporaryAuthority)
//                else if (config.TempAuthorityLevel.HasValue)
//                {
//                    // 逻辑搬运: 如果员工非临时权限大于等于该临时权限,则报错 (Authority值越小权限越大)
//                    // Controller 原文: if (account.AUTHORITY <= dto.tempAuthority) return BadRequest("员工权限已大于等于该临时权限");
//                    if (staffAccount.AUTHORITY <= config.TempAuthorityLevel.Value)
//                    {
//                        throw new Exception("员工权限已大于等于该临时权限");
//                    }

//                    // 如果已存在，先移除旧的 (逻辑搬运: _accountContext.TEMP_AUTHORITY.Remove(existingTempAuthority))
//                    if (existingTempAuth != null)
//                    {
//                        _accountContext.TEMP_AUTHORITY.Remove(existingTempAuth);
//                    }

//                    // 创建临时权限
//                    var tempAuthority = new TempAuthority
//                    {
//                        ACCOUNT = staffAccount.ACCOUNT,
//                        EVENT_ID = config.EventId.Value,
//                        TEMP_AUTHORITY = config.TempAuthorityLevel.Value,
//                    };
//                    await _accountContext.TEMP_AUTHORITY.AddAsync(tempAuthority);
//                }
//            }

//            // 统一保存更改
//            // StaffController 中涉及两个上下文保存: _collabContext 和 _accountContext
//            // 这里我们主要修改了 Account 和 Salary(Collab)，所以两个都要保存
//            await _accountContext.SaveChangesAsync();
//            await _collabContext.SaveChangesAsync();
//        }
//    }
//}
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.patterns.Composite_Pattern.Leaf
{
    /// <summary>
    /// 员工叶子节点 (StaffLeaf)
    /// </summary>
    public class StaffLeaf : IPersonComponent
    {
        // 依赖注入：全部替换为 Repository 接口
        private readonly ICollaborationRepository _collabRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IVenueEventRepository _eventRepo; // 替代原本的 ComplexDbContext
        private readonly int _staffId;

        public StaffLeaf(
            ICollaborationRepository collabRepo,
            IAccountRepository accountRepo,
            IVenueEventRepository eventRepo,
            int staffId)
        {
            _collabRepo = collabRepo;
            _accountRepo = accountRepo;
            _eventRepo = eventRepo;
            _staffId = staffId;
        }

        // =========================================================
        // 1. 全维信息获取 (Read)
        // =========================================================
        public async Task<PersonComponentInfo> GetDetailsAsync()
        {
            // 使用 ICollaborationRepository 替代 CollaborationDbContext
            var staff = await _collabRepo.FindStaffByIdAsync(_staffId);
            if (staff == null) return null;

            return new PersonComponentInfo
            {
                Id = staff.STAFF_ID,
                Name = staff.STAFF_NAME,
                Sex = staff.STAFF_SEX,
                Department = staff.STAFF_APARTMENT,
                Position = staff.STAFF_POSITION,
                Salary = staff.STAFF_SALARY,
                Type = "Employee"
            };
        }

        // =========================================================
        // 2. 档案变更 (Write - Basic Info)
        // =========================================================
        public async Task UpdateProfileAsync(StaffProfileConfig config)
        {
            var staff = await _collabRepo.FindStaffByIdAsync(_staffId);
            if (staff == null) throw new Exception("员工不存在");

            if (config.Name != null) staff.STAFF_NAME = config.Name;
            if (config.Sex != null) staff.STAFF_SEX = config.Sex;
            if (config.Department != null) staff.STAFF_APARTMENT = config.Department;
            if (config.Position != null) staff.STAFF_POSITION = config.Position;
            if (config.BaseSalary.HasValue) staff.STAFF_SALARY = config.BaseSalary.Value;

            // Repository 暴露的 SaveChangesAsync
            await _collabRepo.SaveChangesAsync();
        }

        // =========================================================
        // 3. 薪资管理 (Write - Salary)
        // =========================================================
        public async Task ManageSalaryAsync(SalaryManagementConfig config)
        {
            var staff = await _collabRepo.FindStaffByIdAsync(_staffId);
            if (staff == null) throw new Exception("员工不存在");

            if (config.NewBaseSalary.HasValue)
            {
                staff.STAFF_SALARY = config.NewBaseSalary.Value;
            }

            var salarySlip = await _collabRepo.GetSalarySlipByStaffIdAsync(_staffId, config.MonthTime);
            if (salarySlip == null)
            {
                salarySlip = new SalarySlip
                {
                    STAFF_ID = _staffId,
                    MONTH_TIME = config.MonthTime,
                    ATD_COUNT = 0,
                    BONUS = config.Bonus,
                    FINE = config.Fine
                };
                await _collabRepo.AddSalarySlipAsync(salarySlip);
            }
            else
            {
                salarySlip.BONUS = config.Bonus;
                salarySlip.FINE = config.Fine;
            }

            var monthSalaryCost = await _collabRepo.GetMonthSalaryCostByStaffIdAsync(config.MonthTime);
            // 逻辑保持不变：TotalCost = Salary + Bonus - Fine
            int currentTotal = (int)(staff.STAFF_SALARY + salarySlip.BONUS - salarySlip.FINE);

            if (monthSalaryCost == null)
            {
                monthSalaryCost = new MonthSalaryCost
                {
                    MONTH_TIME = config.MonthTime,
                    TOTAL_COST = currentTotal
                };
                await _collabRepo.AddMonthSalaryCostAsync(monthSalaryCost);
            }
            else
            {
                monthSalaryCost.TOTAL_COST = currentTotal;
            }

            await _collabRepo.SaveChangesAsync();
        }

        public async Task<double> CalculateMonthlyCostAsync(DateTime month)
        {
            var staff = await _collabRepo.FindStaffByIdAsync(_staffId);
            if (staff == null) return 0;

            var salarySlip = await _collabRepo.GetSalarySlipByStaffIdAsync(_staffId, month);
            double baseSalary = staff.STAFF_SALARY;
            double bonus = salarySlip?.BONUS ?? 0;
            double fine = salarySlip?.FINE ?? 0;

            return baseSalary + bonus - fine;
        }

        // =========================================================
        // 4. 权限控制 (Write - Authority)
        // =========================================================
        public async Task ManageAuthorityAsync(AuthorityConfig config)
        {
            // 获取员工关联的账号
            var staffAccount = await _accountRepo.AccountFromStaffID(_staffId);
            if (staffAccount == null) throw new Exception("该员工未绑定账号");

            // 1. 常驻权限变更
            if (config.PermanentAuthorityLevel.HasValue)
            {
                // 检查是否有临时权限 (逻辑保持不变)
                var tempAuthorities = await _accountRepo.FindTempAuthorities(staffAccount.ACCOUNT);
                if (tempAuthorities != null && tempAuthorities.Any())
                {
                    throw new Exception("请先收回临时权限再调整长期权限");
                }
                staffAccount.AUTHORITY = config.PermanentAuthorityLevel.Value;
            }

            // 2. 临时权限变更
            if (config.EventId.HasValue)
            {
                // 验证活动是否存在 (使用 IVenueEventRepository 替代 ComplexDbContext)
                // 这就是引入 _eventRepo 的原因
                var saleEvent = await _eventRepo.FindEventByIdAsync(config.EventId.Value);
                if (saleEvent == null) throw new Exception("活动不存在");

                // 使用 IVenueEventRepository 查找特定活动的权限
                // (AccountRepo 只提供了 FindTempAuthorities 返回 List，不够精确，所以用 EventRepo 的方法)
                var existingTempAuth = await _eventRepo.GetTempAuthorityAsync(staffAccount.ACCOUNT, config.EventId.Value);

                if (config.IsRevokeTemp)
                {
                    // 撤销权限逻辑
                    if (existingTempAuth == null) throw new Exception("临时权限不存在");
                    _eventRepo.RemoveTempAuthority(existingTempAuth);
                }
                else if (config.TempAuthorityLevel.HasValue)
                {
                    // 授予/修改权限逻辑
                    // 权限等级校验 (值越小权限越大)
                    if (staffAccount.AUTHORITY <= config.TempAuthorityLevel.Value)
                    {
                        throw new Exception("员工权限已大于等于该临时权限");
                    }

                    if (existingTempAuth != null)
                    {
                        _eventRepo.RemoveTempAuthority(existingTempAuth);
                    }

                    var tempAuthority = new TempAuthority
                    {
                        ACCOUNT = staffAccount.ACCOUNT,
                        EVENT_ID = config.EventId.Value,
                        TEMP_AUTHORITY = config.TempAuthorityLevel.Value,
                    };
                    await _eventRepo.AddTempAuthorityAsync(tempAuthority);
                }
            }

            // 统一保存
            // 涉及 AccountRepo (常驻权限) 和 EventRepo (临时权限)
            await _accountRepo.SaveChangesAsync();
            await _eventRepo.SaveChangesAsync();
        }
    }
}