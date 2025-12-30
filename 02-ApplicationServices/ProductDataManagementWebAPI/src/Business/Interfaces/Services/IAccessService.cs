using Entities.Enums;

namespace Business.Interfaces.Services
{
    public interface IAccessService
    {
        /// <summary>
        /// Checks if the current user's active tenant matches the provided tenant ID
        /// </summary>
        bool IsActiveTenant(Guid tenantId);

        /// <summary>
        /// Checks if the current user is an active member of the specified tenant
        /// </summary>
        Task<bool> IsTenantMemberAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is an admin of the specified tenant
        /// </summary>
        Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is an admin of the specified tenant.
        /// Does NOT require ActiveTenantId match - allows managing all tenants user is admin of.
        /// Used for tenant management operations like reactivation.
        /// </summary>
        Task<bool> IsTenantAdminOrOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is a member of the specified project
        /// </summary>
        Task<bool> IsProjectMemberAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is an admin of the specified project
        /// </summary>
        Task<bool> IsProjectAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is a project member OR a tenant admin.
        /// This allows tenant admins to access all project details even if not explicitly added as project members.
        /// </summary>
        Task<bool> IsProjectMemberOrAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current user's role in the specified project. Returns null if not a member.
        /// </summary>
        Task<ProjectRole?> GetProjectRoleAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user has at least the specified role in the project.
        /// Role hierarchy: Admin > Editor > Member > Viewer
        /// </summary>
        Task<bool> HasProjectRoleAtLeastAsync(Guid tenantId, Guid projectId, ProjectRole minimumRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user has Editor or Admin role in the specified project
        /// </summary>
        Task<bool> CanEditProjectAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user can edit a specific project file.
        /// User can edit if: (is Editor/Admin AND is file owner) OR (is Editor/Admin AND file is shared with them)
        /// </summary>
        Task<bool> CanEditProjectFileAsync(Guid tenantId, Guid projectId, Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts tenantId and projectId from HttpContext route values.
        /// Returns (Guid.Empty, Guid.Empty) if either parameter cannot be parsed.
        /// </summary>
        (Guid TenantId, Guid ProjectId) GetRouteIds(object? httpContextResource);

        /// <summary>
        /// Extracts tenantId from HttpContext route values.
        /// Returns Guid.Empty if parameter cannot be parsed.
        /// </summary>
        Guid GetRouteTenantId(object? httpContextResource);

        /// <summary>
        /// Checks if current user is authenticated and has a valid ID.
        /// Used by authorization handlers as first validation step.
        /// </summary>
        bool IsUserAuthenticated();

        /// <summary>
        /// Checks if current user has an active tenant set.
        /// Used by authorization handlers as validation step.
        /// </summary>
        bool HasActiveTenant();
    }
}
