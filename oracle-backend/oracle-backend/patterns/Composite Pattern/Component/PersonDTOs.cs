//PersonDTOs.cs
using System;

namespace oracle_backend.patterns.Composite_Pattern.Component
{
    /// <summary>
    /// [读模型] 人员/部门信息快照
    /// 对应 StaffController.GetAllStaffs 的返回需求
    /// </summary>
    public class PersonComponentInfo
    {
        public int Id { get; set; }              // 员工ID (对于部门可以是虚拟ID)
        public string Name { get; set; }         // 员工姓名 或 部门名称
        public string Sex { get; set; }          // 员工性别 (部门为null)
        public string Department { get; set; }   // 所属部门
        public string Position { get; set; }     // 职位
        public double Salary { get; set; }       // 当前基础薪资
        public string Type { get; set; }         // "Employee" 或 "Department"
    }

    /// <summary>
    /// [写模型] 个人档案更新配置
    /// 对应 StaffController.UpdateStaff (2.6.3)
    /// </summary>
    public class StaffProfileConfig
    {
        public string? Name { get; set; }
        public string? Sex { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public double? BaseSalary { get; set; } // 如果只是改信息不改工资，此项可空
    }

    /// <summary>
    /// [写模型] 薪资管理配置
    /// 对应 StaffController.ManageStaffSalary (2.6.5)
    /// </summary>
    public class SalaryManagementConfig
    {
        public DateTime MonthTime { get; set; } // 哪个月的工资
        public double? NewBaseSalary { get; set; } // 是否调整底薪
        public double Bonus { get; set; }       // 奖金
        public double Fine { get; set; }        // 罚金
    }

    /// <summary>
    /// [写模型] 权限管理配置
    /// 对应 StaffController.ModifyStaffAuthority (2.6.2) 和 ManageTemporaryAuthority (2.6.7)
    /// </summary>
    public class AuthorityConfig
    {
        // 常驻权限 (Authority)
        public int? PermanentAuthorityLevel { get; set; }

        // 临时权限 (TempAuthority)
        public int? EventId { get; set; }       // 针对哪个活动
        public int? TempAuthorityLevel { get; set; }
        public bool IsRevokeTemp { get; set; } = false; // 是否是撤销操作
    }
}