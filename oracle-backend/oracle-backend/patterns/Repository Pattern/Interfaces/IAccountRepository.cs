using oracle_backend.Models;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IAccountRepository : IRepository<Account>
    {
        // 对应 AccountDbContext 中的自定义方法
        Task<Account?> FindAccountByUsername(string username);
        Task<bool> CheckAuthority(string account, int goalAuth);
        Task<Account?> AccountFromStaffID(int staffId);
        Task<List<TempAuthority>> FindTempAuthorities(string account);
        Task AddStaffAccountLink(StaffAccount link);
        Task AddStoreAccountLink(StoreAccount link);
        Task<Account?> AccountFromStoreID(int storeId);
        Task<StaffAccount?> CheckStaff(string account);
        Task<StoreAccount?> CheckStore(string account);
        Task<StoreAccount?> GetStoreAccountByAccount(string account);
        Task<IEnumerable<StaffAccount>> GetAllStaffAccountsWithInfoAsync();
        Task<IEnumerable<StoreAccount>> GetAllStoreAccountsWithInfoAsync();
        Task<StoreAccount?> GetStoreAccountWithStoreInfoAsync(string account);

        // 查找特定的关联记录
        Task<StaffAccount?> GetStaffAccountLink(string account, int staffId);
        Task<StoreAccount?> GetStoreAccountLink(string account, int storeId);

        // 移除关联
        void RemoveStaffAccountLink(StaffAccount link);
        void RemoveStoreAccountLink(StoreAccount link);

        // 移除临时权限
        void RemoveTempAuthorities(IEnumerable<TempAuthority> items);
        void RemoveTempAuthority(TempAuthority item);

        // 替代 _context.ACCOUNT.CountAsync()
        Task<int> GetAccountCountAsync();
        // 替代 _context.STAFF.FindAsync()
        Task<Staff?> GetStaffByIdAsync(int id);
        // 替代 _context.STORE.FindAsync()
        Task<Store?> GetStoreByIdAsync(int id);
        // 替代 _context.EVENT.ToListAsync()
        Task<IEnumerable<Event>> GetAllEventsAsync();
    }
}