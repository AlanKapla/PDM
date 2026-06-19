using Entities.Enums;

namespace Business.Interfaces.Services;

public interface IProjectMembershipProvisioner
{
    Task EnsureTenantMemberAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);

    Task ProvisionProjectMemberAsync(
        Guid tenantId,
        Guid projectId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<ProjectModule> modules,
        CancellationToken cancellationToken);

    Task DeactivateAllProjectMembershipsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);
}
