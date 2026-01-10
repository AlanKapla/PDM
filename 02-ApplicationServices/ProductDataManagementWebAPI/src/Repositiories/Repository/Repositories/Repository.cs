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

    public async Task InsertRange(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public Task Update(T entity)
    {
        _context.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRange(IEnumerable<T> entities)
    {
        _context.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task Delete(T entity)
    {
        _context.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRange(IEnumerable<T> entities)
    {
        _context.RemoveRange(entities);
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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    // Query optimization methods
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public async Task<Dictionary<TKey, int>> CountGroupedByAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> groupBy,
        CancellationToken cancellationToken = default) where TKey : notnull
    {
        return await _dbSet
            .Where(predicate)
            .GroupBy(groupBy)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
    }

    // Projection methods for optimized queries
    public async Task<List<TResult>> SelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(predicate)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<TResult>> SelectToHashSetAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(predicate)
            .Select(selector)
            .ToHashSetAsync(cancellationToken);
    }
}
