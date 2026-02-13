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
    }
}
