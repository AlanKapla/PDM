using Entities.Context;
using Entities.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repositiories.Extensions;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task Insert(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public Task Update(T entity)
    {
        _context.Update(entity);
        return Task.CompletedTask;
    }

    public Task Delete(T entity)
    {
        _context.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<T?> GetFirstBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
    {
        return await _dbSet.IncludeMultiple(includes).FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>> GetBySearch(Expression<Func<T, bool>> predicate, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
    {
         return await _dbSet.IncludeMultiple(includes).Where(predicate).ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAll(params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes)
    {
        return await _dbSet.IncludeMultiple(includes).ToListAsync();
    }
}
