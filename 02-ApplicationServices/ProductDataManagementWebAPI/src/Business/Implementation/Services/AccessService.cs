using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services;

public sealed class AccessService : IAccessService
{
    private readonly ILogger<AccessService> logger;

    public AccessService(ILogger<AccessService> logger)
    {
        this.logger = logger;
    }

    public async Task<bool> AuthorizeAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        ResourceScope? resourceScope = null,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsAuthenticated)
        {
            logger.LogWarning("Authorization failed: User not authenticated");
            return false;
        }

        var scope = PermissionScopes.Get(permissionCode);

        // Handle Global scope permissions
        if (scope == PermissionScope.Global)
        {
            logger.LogDebug(
                "Authorization granted: User {UserId} has global permission {Permission}",
                user.Id,
                permissionCode);

            return true;
        }

        // For Tenant and Project scopes, resource must have TenantId
        if (resource.TenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Authorization failed: TenantId is required for permission {Permission}",
                permissionCode);
            return false;
        }

        // Standard flow: resource.TenantId must match user's ActiveTenantId
        // Exception: Some permissions allow cross-tenant access (admin operations)
        if (!IsCrossTenantEnabled(permissionCode))
        {
            // Standard flow - enforce ActiveTenantId match
            if (!user.ActiveTenantId.HasValue || resource.TenantId != user.ActiveTenantId.Value)
            {
                logger.LogWarning(
                    "Authorization failed: Resource TenantId {ResourceTenantId} does not match ActiveTenantId {ActiveTenantId} for permission {Permission}",
                    resource.TenantId,
                    user.ActiveTenantId,
                    permissionCode);
                return false;
            }
        }

        // Route to appropriate authorization method based on scope
        // For cross-tenant enabled permissions, GetTenantSnapshotAsync handles:
        // - Validation that user is Tenant Admin of resource.TenantId
        // - SuperAdmin fallback permissions
        if (scope == PermissionScope.Project)
        {
            return await AuthorizeProjectPermissionAsync(user, permissionCode, resource, resourceScope, cancellationToken);
        }
        else
        {
            return await AuthorizeTenantPermissionAsync(user, permissionCode, resource, cancellationToken);
        }
    }

    /// <summary>
    /// Determines if a permission can work across tenants (for Tenant Admins).
    /// Most permissions require ActiveTenantId match, but some admin operations
    /// allow cross-tenant access (e.g., managing multiple tenants as admin).
    /// </summary>
    private static bool IsCrossTenantEnabled(string permissionCode)
    {
        return permissionCode == PermissionCodes.TenantEdit
            || permissionCode == PermissionCodes.TenantMembersManage
            || permissionCode == PermissionCodes.TenantStatusManage
            || permissionCode == PermissionCodes.ProjectResourcesWriteOwn;
    }

    private async Task<bool> AuthorizeTenantPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        CancellationToken cancellationToken)
    {
        // Use tenant snapshot (cached) - works for both standard flow and cross-tenant
        // GetTenantSnapshotAsync handles:
        // - SuperAdmin fallback permissions
        // - Tenant Admin permissions
        // - Cross-tenant access validation
        var tenantSnapshot = await user.GetTenantSnapshotAsync(resource.TenantId, cancellationToken);
        
        if (tenantSnapshot == null)
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} has no access to tenant {TenantId}",
                user.Id,
                resource.TenantId);
            return false;
        }

        // Check if user has permission
        if (!tenantSnapshot.TenantPermissionCodes.Contains(permissionCode))
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} lacks permission {Permission} in tenant {TenantId}",
                user.Id,
                permissionCode,
                resource.TenantId);
            return false;
        }

        // Check Tenant.IsActive (only for non-admin members)
        // SuperAdmin without membership (fallback permissions) can access inactive tenants if they have the permission
        if (!tenantSnapshot.IsTenantAdmin && !tenantSnapshot.IsActive && !user.IsSuperAdmin)
        {
            logger.LogWarning(
                "Authorization failed: Tenant {TenantId} is inactive and user is not admin or SuperAdmin",
                resource.TenantId);
            return false;
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} in tenant {TenantId}",
            user.Id,
            permissionCode,
            resource.TenantId);

        return true;
    }

    private async Task<bool> AuthorizeProjectPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        ResourceScope? resourceScope,
        CancellationToken cancellationToken)
    {
        if (!resource.ProjectId.HasValue)
        {
            logger.LogWarning("Authorization failed: ProjectId is required but was null");
            return false;
        }

        var projectSnapshot = await user.GetProjectSnapshotAsync(resource.ProjectId.Value, cancellationToken);
        
        if (projectSnapshot == null)
        {
            // GetProjectSnapshotAsync returns null only when user has NO access to the project:
            // - Not a project member
            // - Not a Tenant Admin of project's tenant
            // - Not a SuperAdmin
            logger.LogWarning(
                "Authorization failed: User {UserId} has no access to project {ProjectId}",
                user.Id,
                resource.ProjectId.Value);
            return false;
        }

        // If ResourceScope is specified, validate specific permissions for that scope
        if (resourceScope.HasValue)
        {
            bool hasRequiredPermission = resourceScope.Value switch
            {
                ResourceScope.All => projectSnapshot.ProjectPermissionCodes.Contains(PermissionCodes.ProjectResourcesReadAll),
                ResourceScope.Mine => projectSnapshot.ProjectPermissionCodes.Contains(PermissionCodes.ProjectResourcesRead),
                ResourceScope.Shared => projectSnapshot.ProjectPermissionCodes.Contains(PermissionCodes.ProjectResourcesReadShared),
                _ => false
            };

            if (!hasRequiredPermission)
            {
                logger.LogWarning(
                    "Authorization failed: User {UserId} lacks required permission for ResourceScope {Scope} in project {ProjectId}",
                    user.Id,
                    resourceScope.Value,
                    resource.ProjectId.Value);
                return false;
            }
        }
        else
        {
            // Check if user has the general permission
            if (!projectSnapshot.ProjectPermissionCodes.Contains(permissionCode))
            {
                logger.LogWarning(
                    "Authorization failed: User {UserId} lacks permission {Permission} in project {ProjectId}",
                    user.Id,
                    permissionCode,
                    resource.ProjectId.Value);
                return false;
            }
        }

        // Check Project.IsActive (only for non-admin members)
        // Project admins (including Tenant Admins and SuperAdmins) can access inactive projects
        if (!projectSnapshot.IsProjectAdmin && !projectSnapshot.IsActive && !user.IsSuperAdmin)
        {
            logger.LogWarning(
                "Authorization failed: Project {ProjectId} is inactive and user is not admin or SuperAdmin",
                resource.ProjectId.Value);
            return false;
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} (ResourceScope: {Scope}) in project {ProjectId}",
            user.Id,
            permissionCode,
            resourceScope,
            resource.ProjectId.Value);

        return true;
    }

    /// <summary>
    /// Authorizes access to a resource for the logged-in user without requiring ActiveTenantId.
    /// Used for cross-tenant operations (e.g. "My work") where the user may operate across
    /// multiple tenants simultaneously.
    /// </summary>
    public async Task<bool> AuthorizeAssignedAsync(
        ICurrentUser user,
        string permissionCode,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsAuthenticated)
        {
            logger.LogWarning("AuthorizeAssignedAsync failed: User not authenticated");
            return false;
        }

        var projectSnapshot = await user.GetProjectSnapshotWithoutActiveTenantAsync(projectId, cancellationToken);

        if (projectSnapshot == null)
        {
            logger.LogWarning(
                "AuthorizeAssignedAsync failed: User {UserId} has no access to project {ProjectId}",
                user.Id,
                projectId);
            return false;
        }

        if (!projectSnapshot.ProjectPermissionCodes.Contains(permissionCode))
        {
            logger.LogWarning(
                "AuthorizeAssignedAsync failed: User {UserId} lacks permission {Permission} in project {ProjectId}",
                user.Id,
                permissionCode,
                projectId);
            return false;
        }

        if (!projectSnapshot.IsProjectAdmin && !projectSnapshot.IsActive && !user.IsSuperAdmin)
        {
            logger.LogWarning(
                "AuthorizeAssignedAsync failed: Project {ProjectId} is inactive",
                projectId);
            return false;
        }

        logger.LogDebug(
            "AuthorizeAssignedAsync granted: User {UserId} has permission {Permission} in project {ProjectId}",
            user.Id,
            permissionCode,
            projectId);

        return true;
    }
}
