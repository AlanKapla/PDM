using Entities.Context;
using Entities.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repositories.Extensions;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace Repositories.Repository.Repositories
{
    public class ReadRepository<T>(AppDbContext context) : Repository<T>(context), IReadRepository<T> where T : BaseEntity
    {
        public async Task<T?> GetById(Guid id, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
        {
            return await _dbSet.IncludeMultiple(includes).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.IncludeMultiple(includes);
            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<List<Guid>> GetIdsBySearchAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).Select(x => x.Id).ToListAsync(cancellationToken);
        }

        public async Task<Guid> GetIdBySearchAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return (await _dbSet.FirstAsync(predicate, cancellationToken: cancellationToken)).Id;
        }

        public async Task<Dictionary<Guid, T>> GetDictionaryBySearchAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.IncludeMultiple(includes);
            List<T> entities = await query.Where(predicate).ToListAsync(cancellationToken);
            return entities.ToDictionary(x => x.Id);
        }

        public async Task<List<T>> GetPagedBySearchAsync<TKey>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> orderBy,
            bool descending,
            int skip,
            int take,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.IncludeMultiple(includes).Where(predicate);
            IOrderedQueryable<T> ordered = descending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
            return await ordered.Skip(skip).Take(take).ToListAsync(cancellationToken);
        }

        public async Task<List<T>> GetPagedBySearchAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
            int take,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.IncludeMultiple(includes).Where(predicate);
            IOrderedQueryable<T> ordered = orderBy(query);
            return await ordered.Take(take).ToListAsync(cancellationToken);
        }

        public async Task<List<TResult>> SelectGroupedAsync<TKey, TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> groupBy,
            Expression<Func<IGrouping<TKey, T>, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(predicate)
                .GroupBy(groupBy)
                .Select(selector)
                .ToListAsync(cancellationToken);
        }
    }

}
