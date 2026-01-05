using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class AccessService
{
    private readonly ILogger<AccessService> logger;
    private readonly IReadRepository<Tenant> tenantRepo;
    private readonly IReadRepository<Project> projectRepo;
    private readonly IRepository<TenantMember> tenantMemberRepo;

    public AccessService(
        ILogger<AccessService> logger,
        IReadRepository<Tenant> tenantRepo,
        IReadRepository<Project> projectRepo,
        IRepository<TenantMember> tenantMemberRepo)
    {
        this.logger = logger;
        this.tenantRepo = tenantRepo;
        this.projectRepo = projectRepo;
        this.tenantMemberRepo = tenantMemberRepo;
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
            return await AuthorizeGlobalPermissionAsync(user, permissionCode, cancellationToken);
        }

        // For non-global scopes, check if ActiveTenantId is required
        bool needsActiveTenantId = RequiresActiveTenantId(scope, permissionCode);
        
        if (needsActiveTenantId && !user.ActiveTenantId.HasValue)
        {
            logger.LogWarning(
                "Authorization failed: ActiveTenantId required for permission {Permission} with scope {Scope}", 
                permissionCode, 
                scope);
            return false;
        }

        // For permissions that don't require ActiveTenantId, resource MUST have TenantId
        if (!needsActiveTenantId && resource.TenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Authorization failed: Resource TenantId required for permission {Permission}",
                permissionCode);
            return false;
        }

        // Validate ActiveTenantId matches resource TenantId (if both are set)
        // Exception: Some tenant management permissions can work across different tenants
        if (user.ActiveTenantId.HasValue && resource.TenantId != Guid.Empty 
            && user.ActiveTenantId.Value != resource.TenantId
            && !CanWorkAcrossTenants(permissionCode))
        {
            logger.LogWarning(
                "Authorization failed: ActiveTenantId {ActiveTenantId} does not match resource TenantId {ResourceTenantId}",
                user.ActiveTenantId.Value,
                resource.TenantId);
            return false;
        }

        // Get appropriate context snapshot (works for both regular users and SuperAdmin)
        // SuperAdmin will get membership-based permissions if they are a member,
        // or fallback read-only permissions if they are not
        if (resource.ProjectId.HasValue || scope == PermissionScope.Project)
        {
            return await AuthorizeProjectPermissionAsync(user, permissionCode, resource, resourceScope, cancellationToken);
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
        if (permissionCode == PermissionCodes.TenantListAvailable
            || permissionCode == PermissionCodes.TenantAdminListAvailable
            || permissionCode == PermissionCodes.RoleList)
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
        Guid tenantIdToCheck = resource.TenantId != Guid.Empty ? resource.TenantId : user.ActiveTenantId!.Value;

        // For permissions that don't require ActiveTenantId, load membership directly for the specific tenant
        if (IsActiveTenantIdOptional(permissionCode) && resource.TenantId != Guid.Empty)
        {
            // Load membership directly from database (not cached snapshot)
            var membership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == resource.TenantId
                     && tm.UserId == user.Id
                     && tm.IsActive,
                q => q.Include(tm => tm.MemberRole).Include(tm => tm.Tenant)
            );

            if (membership == null)
            {
                logger.LogWarning(
                    "Authorization failed: User {UserId} is not a member of tenant {TenantId}",
                    user.Id,
                    resource.TenantId);
                return false;
            }

            // Check if user is admin of this tenant
            if (membership.MemberRole?.Code != RoleCodes.TenantAdmin)
            {
                logger.LogWarning(
                    "Authorization failed: User {UserId} is not admin of tenant {TenantId}",
                    user.Id,
                    resource.TenantId);
                return false;
            }

            // Check if tenant is active (only for non-admins, but since we already know user is admin, skip)
            logger.LogDebug(
                "Authorization granted: User {UserId} has permission {Permission} in tenant {TenantId} (cross-tenant check)",
                user.Id,
                permissionCode,
                resource.TenantId);
            
            return true;
        }

        // Standard flow: use cached snapshot
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
                tenantIdToCheck);
            return false;
        }

        // Check Tenant.IsActive (only for non-admin members)
        // SuperAdmin without membership (fallback permissions) can access inactive tenants if they have the permission
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

            // Skip IsActive check for SuperAdmin (even without admin membership)
            if (!tenant.IsActive && !user.IsSuperAdmin)
            {
                logger.LogWarning(
                    "Authorization failed: Tenant {TenantId} is inactive and user is not admin or SuperAdmin",
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
        // SuperAdmin without membership (fallback permissions) can access inactive projects if they have the permission
        if (!projectSnapshot.IsProjectAdmin)
        {
            var project = await projectRepo.GetFirstBySearch(
                p => p.Id == resource.ProjectId.Value,
                cancellationToken);

            if (project == null)
            {
                logger.LogWarning("Authorization failed: Project {ProjectId} not found", resource.ProjectId.Value);
                return false;
            }

            // Skip IsActive check for SuperAdmin (even without admin membership)
            if (!project.IsActive && !user.IsSuperAdmin)
            {
                logger.LogWarning(
                    "Authorization failed: Project {ProjectId} is inactive and user is not admin or SuperAdmin",
                    resource.ProjectId.Value);
                return false;
            }
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} (ResourceScope: {Scope}) in project {ProjectId}",
            user.Id,
            permissionCode,
            resourceScope,
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

    private static bool RequiresActiveTenantId(PermissionScope scope, string permissionCode)
    {
        // Global scope never requires ActiveTenantId
        if (scope == PermissionScope.Global)
            return false;

        // These tenant management permissions don't require ActiveTenantId
        return !IsActiveTenantIdOptional(permissionCode);
    }

    private static bool IsActiveTenantIdOptional(string permissionCode)
    {
        // Permissions that work without ActiveTenantId (tenant management operations)
        return permissionCode == PermissionCodes.TenantEdit
            || permissionCode == PermissionCodes.TenantMembersManage
            || permissionCode == PermissionCodes.TenantStatusManage;
    }

    private static bool CanWorkAcrossTenants(string permissionCode)
    {
        // Permissions that can work on different tenant than ActiveTenantId
        // (as long as user is admin of that tenant)
        return IsActiveTenantIdOptional(permissionCode);
    }
}
