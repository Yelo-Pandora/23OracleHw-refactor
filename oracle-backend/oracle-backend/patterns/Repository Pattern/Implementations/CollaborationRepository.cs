using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class CollaborationRepository : BaseRepository<Collaboration>, ICollaborationRepository
    {
        // 最主要引用具体的 CollaborationDbContext
        private readonly CollaborationDbContext _collabContext;

        public CollaborationRepository(CollaborationDbContext context) : base(context)
        {
            _collabContext = context;
        }

        // 封装 CollaborationDbContext 原有方法

        public async Task<Staff?> FindStaffByAccountAsync(string account)
        {
            return await _collabContext.FindStaffByAccount(account);
        }

        public async Task<Staff?> FindStaffByIdAsync(int staffId)
        {
            return await _collabContext.FindStaffById(staffId);
        }

        public async Task<SalarySlip?> GetSalarySlipByStaffIdAsync(int staffId, DateTime monthTime)
        {
            return await _collabContext.GetSalarySlipByStaffId(staffId, monthTime);
        }

        public async Task<MonthSalaryCost?> GetMonthSalaryCostByStaffIdAsync(DateTime monthTime)
        {
            return await _collabContext.GetMonthSalaryCostByStaffId(monthTime);
        }

        // 实现 Staff 相关的辅助操作

        public async Task<IEnumerable<Staff>> GetAllStaffsAsync()
        {
            return await _collabContext.Staffs.ToListAsync();
        }

        public async Task<int> GetMaxStaffIdAsync()
        {
            // 如果表为空，MaxAsync 会抛出异常或返回 null，需处理空表情况
            if (await _collabContext.Staffs.CountAsync() == 0)
            {
                return 0;
            }
            return await _collabContext.Staffs.MaxAsync(s => s.STAFF_ID);
        }

        public async Task AddStaffAsync(Staff staff)
        {
            await _collabContext.Staffs.AddAsync(staff);
        }

        // 实现 薪资 相关的辅助操作
        public async Task<IEnumerable<SalarySlip>> GetAllSalarySlipsAsync()
        {
            return await _collabContext.SalarySlips.ToListAsync();
        }

        public async Task<IEnumerable<MonthSalaryCost>> GetAllMonthSalaryCostsAsync()
        {
            return await _collabContext.MonthSalaryCosts.ToListAsync();
        }

        public async Task AddSalarySlipAsync(SalarySlip salarySlip)
        {
            await _collabContext.SalarySlips.AddAsync(salarySlip);
        }

        public async Task AddMonthSalaryCostAsync(MonthSalaryCost monthSalaryCost)
        {
            await _collabContext.MonthSalaryCosts.AddAsync(monthSalaryCost);
        }

        // 实现 合作方 相关的特定检查
        public async Task<bool> ExistsAsync(int id)
        {
            return await _collabContext.Collaborations
                .AnyAsync(c => c.COLLABORATION_ID == id);
        }

        public async Task<bool> HasActiveEventsAsync(int collaborationId)
        {
            return await _collabContext.VenueEventDetails
                .AnyAsync(v => v.COLLABORATION_ID == collaborationId && v.STATUS == "ACTIVE"); // 或其他表示进行中的状态
        }

        public async Task<IEnumerable<dynamic>> GetCollaborationReportAsync(DateTime startDate, DateTime endDate, string? industry)
        {
            var query = _collabContext.VenueEventDetails
                .Where(ved => ved.RENT_START >= startDate && ved.RENT_END <= endDate);

            if (!string.IsNullOrWhiteSpace(industry))
            {
                string pattern = $"%{industry.Trim()}%";
                query = query.Where(ved =>
                    EF.Functions.Like(ved.collaborationNavigation.COLLABORATION_NAME, pattern));
            }

            return await query
                .GroupBy(ved => new { ved.COLLABORATION_ID })
                .Select(g => new
                {
                    CollaborationId = g.Key.COLLABORATION_ID,
                    EventCount = g.Count(),
                    TotalInvestment = g.Sum(x => x.FUNDING),
                    AvgRevenue = g.Average(x => x.FUNDING)
                })
                .ToListAsync();
        }
    }
}