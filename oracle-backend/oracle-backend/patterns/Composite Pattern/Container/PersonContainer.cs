//PersonContainer.cs
using oracle_backend.patterns.Composite_Pattern.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oracle_backend.patterns.Composite_Pattern.Container
{
    /// <summary>
    /// 部门容器节点 (DepartmentComposite)
    /// 职责：聚合下属员工/子部门，执行广播操作或汇总计算。
    /// </summary>
    public class DepartmentComposite : IPersonComponent
    {
        private readonly List<IPersonComponent> _children = new List<IPersonComponent>();
        private readonly string _departmentName;

        public DepartmentComposite(string departmentName)
        {
            _departmentName = departmentName;
        }

        public void Add(IPersonComponent component) => _children.Add(component);
        public void Remove(IPersonComponent component) => _children.Remove(component);

        // =========================================================
        // 1. 全维信息获取 (Read)
        // 对应: StaffController.GetAllStaffs (聚合视角)
        // =========================================================
        public Task<PersonComponentInfo> GetDetailsAsync()
        {
            // Container 自身的信息快照
            // 不做复杂的平均工资计算，只返回基础身份标识
            return Task.FromResult(new PersonComponentInfo
            {
                Id = 0, // 虚拟ID
                Name = _departmentName,
                Sex = null, // 部门无性别
                Department = _departmentName,
                Position = "Department",
                Salary = 0, // 部门本身无底薪
                Type = "Department"
            });
        }

        // =========================================================
        // 2. 档案变更 (Write - Basic Info)
        // 对应: StaffController.UpdateStaff
        // =========================================================
        public async Task UpdateProfileAsync(StaffProfileConfig config)
        {
            // 广播操作：如果是部门级别的更新（例如全员调岗？），则分发给所有子节点
            // 如果 Controller 没有这个业务场景，这里可以留空或仅遍历
            // 在 StaffController 中，UpdateStaff 是针对单个 StaffId 的，通常直接调 Leaf
            // 但作为 Composite 模式，标准行为是递归调用
            foreach (var child in _children)
            {
                await child.UpdateProfileAsync(config);
            }
        }

        // =========================================================
        // 3. 薪资管理 (Write) & 成本计算 (Read)
        // 对应: StaffController.ManageStaffSalary, GetAllMonthSalaryCost
        // =========================================================

        // [写] 广播调薪 (例如全部门发奖金)
        public async Task ManageSalaryAsync(SalaryManagementConfig config)
        {
            foreach (var child in _children)
            {
                await child.ManageSalaryAsync(config);
            }
        }

        // [读] 计算部门总成本 (Sum)
        // 逻辑来源: 虽然 Controller 中 GetAllMonthSalaryCost 是取列表，
        // 但财务视角的"部门成本"定义必然是下属之和。
        public async Task<double> CalculateMonthlyCostAsync(DateTime month)
        {
            double totalCost = 0;
            foreach (var child in _children)
            {
                totalCost += await child.CalculateMonthlyCostAsync(month);
            }
            return totalCost;
        }

        // =========================================================
        // 4. 权限控制 (Write - Authority)
        // 对应: StaffController.ModifyStaffAuthority
        // =========================================================

        // [写] 广播权限变更 (例如给整个部门开通某个活动的临时权限)
        public async Task ManageAuthorityAsync(AuthorityConfig config)
        {
            foreach (var child in _children)
            {
                // 只有当操作成功时才继续，或者可以由调用者决定是否忽略个别失败
                await child.ManageAuthorityAsync(config);
            }
        }
    }
}
