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
    }
}
