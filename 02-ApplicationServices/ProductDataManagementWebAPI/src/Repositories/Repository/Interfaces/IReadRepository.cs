using Entities.Models.Base;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Repositories.Repository.Interfaces
{
    public interface IReadRepository<T> : IRepository<T> where T : BaseEntity
    {
        Task<T?> GetById(Guid id, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
    }
}
