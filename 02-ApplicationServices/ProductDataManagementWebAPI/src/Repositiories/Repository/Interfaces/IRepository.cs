using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Repositories.Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task Insert(T entity);
        Task Update(T entity);
        Task Delete(T entity);
        Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<IEnumerable<T>> GetBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
        Task<IEnumerable<T>> GetAll(params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes);
    }
}
