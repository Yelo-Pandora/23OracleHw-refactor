using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Patterns.Repository.Interfaces;

namespace oracle_backend.Patterns.Repository.Implementations
{
    public class AccountRepository : BaseRepository<Account>, IAccountRepository
    {
        // 强类型引用具体的 DbContext
        private readonly AccountDbContext _accountContext;

        public AccountRepository(AccountDbContext context) : base(context)
        {
            _accountContext = context;
        }

        // --- 包装 DbContext 中的现有方法 ---
        public async Task<Account?> FindAccountByUsername(string username)
            => await _accountContext.FindAccount(username);

        public async Task<bool> CheckAuthority(string account, int goalAuth)
            => await _accountContext.CheckAuthority(account, goalAuth);

        public async Task<Account?> AccountFromStaffID(int staffId)
            => await _accountContext.AccountFromStaffID(staffId);

        public async Task<List<TempAuthority>> FindTempAuthorities(string account)
            => await _accountContext.FindTempAuthorities(account);

        // --- 处理关联表 (StaffAccount, StoreAccount) ---
        // 既然不改 DbContext，我们可以在 Repo 里直接操作这些 DbSet
        public async Task AddStaffAccountLink(StaffAccount link)
            => await _accountContext.STAFF_ACCOUNT.AddAsync(link);

        public async Task AddStoreAccountLink(StoreAccount link)
            => await _accountContext.STORE_ACCOUNT.AddAsync(link);

        public async Task<Account?> AccountFromStoreID(int storeId)
            => await _accountContext.AccountFromStoreID(storeId);

        public async Task<StaffAccount?> CheckStaff(string account)
            => await _accountContext.CheckStaff(account);

        public async Task<StoreAccount?> CheckStore(string account)
            => await _accountContext.CheckStore(account);

        public async Task<StoreAccount?> GetStoreAccountByAccount(string account)
            => await _accountContext.GetStoreAccountByAccount(account);

        public async Task<IEnumerable<StaffAccount>> GetAllStaffAccountsWithInfoAsync()
        {
            // 直接使用 AccountDbContext 中的 DbSet 并 Include
            return await _accountContext.STAFF_ACCOUNT
                .Include(sa => sa.staffNavigation) // 预加载 Staff 信息
                .ToListAsync();
        }

        public async Task<IEnumerable<StoreAccount>> GetAllStoreAccountsWithInfoAsync()
        {
            return await _accountContext.STORE_ACCOUNT
                .Include(sa => sa.storeNavigation) // 预加载 Store 信息
                .ToListAsync();
        }

        // 关联表增删查操作

        public async Task<StaffAccount?> GetStaffAccountLink(string account, int staffId)
            => await _accountContext.STAFF_ACCOUNT
                .FirstOrDefaultAsync(sa => sa.ACCOUNT == account && sa.STAFF_ID == staffId);

        public async Task<StoreAccount?> GetStoreAccountLink(string account, int storeId)
            => await _accountContext.STORE_ACCOUNT
                .FirstOrDefaultAsync(sa => sa.ACCOUNT == account && sa.STORE_ID == storeId);

        public void RemoveStaffAccountLink(StaffAccount link)
            => _accountContext.STAFF_ACCOUNT.Remove(link);

        public void RemoveStoreAccountLink(StoreAccount link)
            => _accountContext.STORE_ACCOUNT.Remove(link);

        public void RemoveTempAuthorities(IEnumerable<TempAuthority> items)
            => _accountContext.TEMP_AUTHORITY.RemoveRange(items);

        public void RemoveTempAuthority(TempAuthority item)
            => _accountContext.TEMP_AUTHORITY.Remove(item);

        // 辅助查询实现
        public async Task<int> GetAccountCountAsync()
        {
            return await _accountContext.ACCOUNT.CountAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(int id)
        {
            return await _accountContext.STAFF.FirstOrDefaultAsync(s => s.STAFF_ID == id);
        }

        public async Task<Store?> GetStoreByIdAsync(int id)
        {
            return await _accountContext.STORE.FirstOrDefaultAsync(s => s.STORE_ID == id);
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _accountContext.EVENT.ToListAsync();
        }
        public async Task<StoreAccount?> GetStoreAccountWithStoreInfoAsync(string account)
        {
            return await _accountContext.STORE_ACCOUNT
                .Include(sa => sa.storeNavigation) // 👈 显式加载 Store
                .FirstOrDefaultAsync(sa => sa.ACCOUNT == account);
        }
    }
}