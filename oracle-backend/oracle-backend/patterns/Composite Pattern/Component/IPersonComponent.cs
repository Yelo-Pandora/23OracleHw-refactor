//IPersonComponent.cs
using System;
using System.Threading.Tasks;

namespace oracle_backend.patterns.Composite_Pattern.Component
{
    public interface IPersonComponent
    {
        // =========================================================
        // 1. 全维信息获取 (Read)
        // 对应: StaffController.GetAllStaffs, CheckPermission
        // =========================================================
        
        /// <summary>
        /// 获取组件的完整快照。
        /// 包含：基本信息(Name/Dept)、当前权限等级(Authority)、当前薪资水平(BaseSalary)。
        /// Leaf: 返回单个员工详情。
        /// Container: 返回部门概况（如部门名、人数汇总）。
        /// </summary>
        Task<PersonComponentInfo> GetDetailsAsync();


        // =========================================================
        // 2. 档案变更 (Write - Basic Info)
        // 对应: StaffController.UpdateStaff
        // =========================================================

        /// <summary>
        /// 更新基本档案（不含薪资和权限）。
        /// </summary>
        Task UpdateProfileAsync(StaffProfileConfig config);


        // =========================================================
        // 3. 薪资管理 (Write - Salary) & 成本计算 (Read - Financial)
        // 对应: StaffController.ManageStaffSalary, GetAllMonthSalaryCost
        // =========================================================

        /// <summary>
        /// [写操作] 执行薪资变动（设置底薪、发放奖金/罚金）。
        /// Leaf: 更新数据库记录。
        /// Container: 广播操作（如全员发奖金）。
        /// </summary>
        Task ManageSalaryAsync(SalaryManagementConfig config);

        /// <summary>
        /// [读操作] 计算指定月份的实际人力成本支出。
        /// Leaf: Base + Bonus - Fine
        /// Container: Sum(Children.Cost)
        /// </summary>
        Task<double> CalculateMonthlyCostAsync(DateTime month);


        // =========================================================
        // 4. 权限控制 (Write - Authority)
        // 对应: StaffController.ModifyStaffAuthority, ManageTemporaryAuthority
        // =========================================================

        /// <summary>
        /// [写操作] 变更权限（常驻或临时）。
        /// Leaf: 修改 Account/TempAuthority 表。
        /// Container: 广播操作（全员开通权限）。
        /// </summary>
        Task ManageAuthorityAsync(AuthorityConfig config);
    }
}
