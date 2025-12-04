using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface ICollaborationRepository : IRepository<Collaboration>
    {
        // 封装 CollaborationDbContext 中的自定义方法
        Task<Staff?> FindStaffByAccountAsync(string account);
        Task<Staff?> FindStaffByIdAsync(int staffId);
        Task<SalarySlip?> GetSalarySlipByStaffIdAsync(int staffId, DateTime monthTime);
        Task<MonthSalaryCost?> GetMonthSalaryCostByStaffIdAsync(DateTime monthTime);

        // 简化StaffController内的查询
        // 获取所有员工
        Task<IEnumerable<Staff>> GetAllStaffsAsync();
        // 获取最大员工ID (用于生成新ID)
        Task<int> GetMaxStaffIdAsync();
        // 添加员工
        Task AddStaffAsync(Staff staff);

        // 补充薪资相关的操作
        Task<IEnumerable<SalarySlip>> GetAllSalarySlipsAsync();
        Task<IEnumerable<MonthSalaryCost>> GetAllMonthSalaryCostsAsync();
        Task AddSalarySlipAsync(SalarySlip salarySlip);
        Task AddMonthSalaryCostAsync(MonthSalaryCost monthSalaryCost);

        // 补充合作方相关的业务检查
        // 检查是否存在ID
        Task<bool> ExistsAsync(int id);
        // 检查是否有活跃的活动 (用于删除时的校验)
        Task<bool> HasActiveEventsAsync(int collaborationId);
        Task<IEnumerable<dynamic>> GetCollaborationReportAsync(DateTime startDate, DateTime endDate, string? industry);
    }
}