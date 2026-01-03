using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class AccessService
{
    private readonly ILogger<AccessService> logger;
    private readonly IReadRepository<Tenant> tenantRepo;
    private readonly IReadRepository<Project> projectRepo;

    public AccessService(
        ILogger<AccessService> logger,
        IReadRepository<Tenant> tenantRepo,
        IReadRepository<Project> projectRepo)
    {
        this.logger = logger;
        this.tenantRepo = tenantRepo;
        this.projectRepo = projectRepo;
    }

    public async Task<bool> AuthorizeAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsAuthenticated)
        {
            logger.LogWarning("Authorization failed: User not authenticated");
            return false;
        }

        var scope = PermissionScopes.Get(permissionCode);

        // SuperAdmin bypass (but still needs ActiveTenantId if scope requires it)
        if (user.IsSuperAdmin)
        {
            if (RequiresActiveTenantId(scope) && !user.ActiveTenantId.HasValue)
            {
                logger.LogWarning(
                    "Authorization failed: SuperAdmin missing ActiveTenantId for permission {Permission} with scope {Scope}", 
                    permissionCode, 
                    scope);
                return false;
            }
            
            logger.LogDebug("Authorization granted: SuperAdmin bypass for {Permission} (scope: {Scope})", permissionCode, scope);
            return true;
        }

        // Handle Global scope permissions
        if (scope == PermissionScope.Global)
        {
            return await AuthorizeGlobalPermissionAsync(user, permissionCode, cancellationToken);
        }

        // For non-global scopes, ActiveTenantId is required
        if (!user.ActiveTenantId.HasValue)
        {
            logger.LogWarning(
                "Authorization failed: ActiveTenantId required for permission {Permission} with scope {Scope}", 
                permissionCode, 
                scope);
            return false;
        }

        // Validate ActiveTenantId matches resource TenantId (if resource has TenantId set)
        if (resource.TenantId != Guid.Empty && user.ActiveTenantId.Value != resource.TenantId)
        {
            logger.LogWarning(
                "Authorization failed: ActiveTenantId {ActiveTenantId} does not match resource TenantId {ResourceTenantId}",
                user.ActiveTenantId.Value,
                resource.TenantId);
            return false;
        }

        // Get appropriate context snapshot
        if (resource.ProjectId.HasValue || scope == PermissionScope.Project)
        {
            return await AuthorizeProjectPermissionAsync(user, permissionCode, resource, cancellationToken);
        }
        else
        {
            return await AuthorizeTenantPermissionAsync(user, permissionCode, resource, cancellationToken);
        }
    }

    private Task<bool> AuthorizeGlobalPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        // For global permissions, we check if user has the permission in ANY of their tenants
        // This is used for operations like listing available tenants
        
        // For TENANT.LIST.AVAILABLE, any authenticated user should have access
        if (permissionCode == PermissionCodes.TenantListAvailable)
        {
            logger.LogDebug(
                "Authorization granted: User {UserId} has global permission {Permission}",
                user.Id,
                permissionCode);
            return Task.FromResult(true);
        }

        // For other global permissions, check if user has it in any tenant context
        // This would require loading user's tenant memberships
        logger.LogWarning(
            "Authorization failed: Global permission {Permission} not implemented for user {UserId}",
            permissionCode,
            user.Id);
        return Task.FromResult(false);
    }

    private async Task<bool> AuthorizeTenantPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        CancellationToken cancellationToken)
    {
        var tenantSnapshot = await user.GetActiveTenantSnapshotAsync(cancellationToken);
        
        if (tenantSnapshot == null)
        {
            logger.LogWarning("Authorization failed: User {UserId} has no tenant context", user.Id);
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

        // Determine which tenantId to check (from resource or from user's active tenant)
        var tenantIdToCheck = resource.TenantId != Guid.Empty ? resource.TenantId : user.ActiveTenantId!.Value;

        // Check Tenant.IsActive (only for non-admin members)
        if (!tenantSnapshot.IsTenantAdmin)
        {
            var tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == tenantIdToCheck,
                cancellationToken);

            if (tenant == null)
            {
                logger.LogWarning("Authorization failed: Tenant {TenantId} not found", tenantIdToCheck);
                return false;
            }

            if (!tenant.IsActive)
            {
                logger.LogWarning(
                    "Authorization failed: Tenant {TenantId} is inactive and user is not admin",
                    tenant.Id);
                return false;
            }
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} in tenant {TenantId}",
            user.Id,
            permissionCode,
            tenantIdToCheck);
        
        return true;
    }

    private async Task<bool> AuthorizeProjectPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
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
            // User might still have tenant-level permission
            var tenantSnapshot = await user.GetActiveTenantSnapshotAsync(cancellationToken);
            
            if (tenantSnapshot != null && tenantSnapshot.IsTenantAdmin && tenantSnapshot.TenantPermissionCodes.Contains(permissionCode))
            {
                logger.LogDebug(
                    "Authorization granted: User {UserId} has tenant admin permission {Permission}",
                    user.Id,
                    permissionCode);
                return await CheckProjectActiveAsync(resource.ProjectId.Value, false, cancellationToken);
            }

            logger.LogWarning("Authorization failed: User {UserId} has no project context for {ProjectId}", user.Id, resource.ProjectId.Value);
            return false;
        }

        // Check if user has permission
        if (!projectSnapshot.ProjectPermissionCodes.Contains(permissionCode))
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} lacks permission {Permission} in project {ProjectId}",
                user.Id,
                permissionCode,
                resource.ProjectId.Value);
            return false;
        }

        // Check Project.IsActive (only for non-admin members)
        if (!projectSnapshot.IsProjectAdmin)
        {
            var isActive = await CheckProjectActiveAsync(resource.ProjectId.Value, true, cancellationToken);
            
            if (!isActive)
            {
                logger.LogWarning(
                    "Authorization failed: Project {ProjectId} is inactive and user is not admin",
                    resource.ProjectId.Value);
                return false;
            }
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} in project {ProjectId}",
            user.Id,
            permissionCode,
            resource.ProjectId.Value);
        
        return true;
    }

    private async Task<bool> CheckProjectActiveAsync(Guid projectId, bool required, CancellationToken cancellationToken)
    {
        var project = await projectRepo.GetFirstBySearch(
            p => p.Id == projectId,
            cancellationToken);

        if (project == null)
        {
            logger.LogWarning("Project {ProjectId} not found", projectId);
            return false;
        }

        return !required || project.IsActive;
    }

    private static bool RequiresActiveTenantId(PermissionScope scope)
    {
        return scope != PermissionScope.Global;
    }
}
