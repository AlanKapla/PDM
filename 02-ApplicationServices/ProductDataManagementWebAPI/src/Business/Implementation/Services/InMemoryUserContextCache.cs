using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Implementation.Services;

public sealed class InMemoryUserContextCache : IUserContextCache
{
    private readonly IMemoryCache cache;

    public InMemoryUserContextCache(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public async Task<int> GetOrCreateUserPermissionsVersionAsync(
        Guid userId,
        Func<Task<int>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var key = $"user:permissions-version:{userId}";
        
        var result = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        });
        
        return result;
    }

    public async Task<HashSet<string>> GetOrCreateRolePermissionsAsync(
        Guid roleId,
        Func<Task<HashSet<string>>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var key = $"role:permissions:{roleId}";
        
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        }) ?? await factory();
    }

    public async Task<TenantCtxSnapshot> GetOrCreateTenantCtxAsync(
        Guid userId,
        Guid tenantId,
        int version,
        Func<Task<TenantCtxSnapshot>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var key = $"tenant:ctx:{userId}:{tenantId}:{version}";
        
        var result = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        });

        return result ?? await factory();
    }

    public async Task<ProjectCtxSnapshot> GetOrCreateProjectCtxAsync(
        Guid userId,
        Guid tenantId,
        Guid projectId,
        int version,
        Func<Task<ProjectCtxSnapshot>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var key = $"project:ctx:{userId}:{tenantId}:{projectId}:{version}";
        
        var result = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        });

        return result ?? await factory();
    }

    public void InvalidateUserPermissionsVersion(Guid userId)
    {
        var key = $"user:permissions-version:{userId}";
        cache.Remove(key);
    }

    public void InvalidateTenantContext(Guid userId, Guid tenantId)
    {
        var pattern = $"tenant:ctx:{userId}:{tenantId}:";
        // Note: IMemoryCache doesn't support pattern-based removal
        // Version bump will naturally invalidate old entries
    }

    public void InvalidateProjectContext(Guid userId, Guid projectId)
    {
        var pattern = $"project:ctx:{userId}:*:{projectId}:";
        // Note: IMemoryCache doesn't support pattern-based removal
        // Version bump will naturally invalidate old entries
    }
}
