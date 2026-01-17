using Business.Interfaces.Model;

namespace Business.Interfaces.Services;

public interface IUserContextCache
{
    Task<int> GetOrCreateUserPermissionsVersionAsync(
        Guid userId,
        Func<Task<int>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetOrCreateRolePermissionsAsync(
        Guid roleId,
        Func<Task<HashSet<string>>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<TenantCtxSnapshot> GetOrCreateTenantCtxAsync(
        Guid userId,
        Guid tenantId,
        int version,
        Func<Task<TenantCtxSnapshot>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<ProjectCtxSnapshot> GetOrCreateProjectCtxAsync(
        Guid userId,
        Guid tenantId,
        Guid projectId,
        int version,
        Func<Task<ProjectCtxSnapshot>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    void InvalidateUserPermissionsVersion(Guid userId);
    void InvalidateTenantContext(Guid userId, Guid tenantId);
    void InvalidateProjectContext(Guid userId, Guid projectId);
}
