using System.Linq.Expressions;

namespace E_commerce.Api.Abstractions
{
    public interface IRepository<T> where T : class
    {
        // Query operations
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        // Paging
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true, CancellationToken cancellationToken = default);

        // CRUD operations
        Task<T> AddAsync(T entity, CancellationToken cancellationToken);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
        void Update(T entity, CancellationToken cancellationToken);
        void UpdateRange(IEnumerable<T> entities, CancellationToken cancellationToken);
        void Remove(T entity, CancellationToken cancellationToken);
        void RemoveRange(IEnumerable<T> entities, CancellationToken cancellationToken);

        // Save changes
        Task<int> SaveChangesAsync();
    }
}
