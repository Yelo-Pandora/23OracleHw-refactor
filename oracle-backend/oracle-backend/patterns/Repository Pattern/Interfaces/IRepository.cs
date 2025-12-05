using System.Linq.Expressions;

namespace oracle_backend.Patterns.Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> GetByIdAsync(object id);
        Task AddAsync(T entity);
        void Remove(T entity);
        void Update(T entity);
        Task<bool> SaveChangesAsync(); // 将保存操作暴露出来，供Controller使用
    }
}
