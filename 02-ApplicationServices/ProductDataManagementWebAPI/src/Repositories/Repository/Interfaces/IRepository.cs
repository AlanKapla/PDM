using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Repositories.Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task Insert(T entity);
        Task InsertRange(IEnumerable<T> entities);
        Task Update(T entity);
        Task UpdateRange(IEnumerable<T> entities);
        Task Delete(T entity);
        Task DeleteRange(IEnumerable<T> entities);
        Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<IEnumerable<T>> GetBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<IEnumerable<T>> GetAll(params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        // Query optimization methods
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Dictionary<TKey, int>> CountGroupedByAsync<TKey>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> groupBy,
            CancellationToken cancellationToken = default) where TKey : notnull;
        
        // Projection methods for optimized queries
        Task<List<TResult>> SelectAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default);
        
        Task<HashSet<TResult>> SelectToHashSetAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default);
    }
}
