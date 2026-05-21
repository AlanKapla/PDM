using Entities.Models.Base;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Repositories.Repository.Interfaces
{
    public interface IReadRepository<T> : IRepository<T> where T : BaseEntity
    {
        Task<T?> GetById(Guid id, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        
        /// <summary>
        /// Pobiera tylko IDs encji spełniających warunek (bez ładowania całych obiektów)
        /// Wydajne dla dużych zbiorów danych
        /// </summary>
        Task<List<Guid>> GetIdsBySearchAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobiera encje spełniające warunek jako słownik [Id -> Entity]
        /// Wydajne dla operacji lookup po ID
        /// </summary>
        Task<Dictionary<Guid, T>> GetDictionaryBySearchAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<Guid> GetIdBySearchAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a page of entities filtered by predicate, ordered by orderBy expression,
        /// with paging applied at the database level (translates to SQL OFFSET/FETCH).
        /// </summary>
        Task<List<T>> GetPagedBySearchAsync<TKey>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> orderBy,
            bool descending,
            int skip,
            int take,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);

        /// <summary>
        /// Returns a page of entities filtered by predicate, ordered by a composite
        /// orderBy delegate (allows ThenBy / ThenByDescending), with TOP applied at
        /// the database level. Used for keyset / cursor pagination where the WHERE
        /// clause already encodes the cursor position.
        /// </summary>
        Task<List<T>> GetPagedBySearchAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
            int take,
            CancellationToken cancellationToken = default,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);

        /// <summary>
        /// Groups filtered entities by the given key and projects each group to a result.
        /// Translates to a single SQL statement (GROUP BY / OUTER APPLY) — no entities
        /// are materialized in memory before projection.
        /// </summary>
        Task<List<TResult>> SelectGroupedAsync<TKey, TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> groupBy,
            Expression<Func<IGrouping<TKey, T>, TResult>> selector,
            CancellationToken cancellationToken = default);
    }
}
