namespace Business.Interfaces.Constants;

/// <summary>
/// Maps permission codes to their required scope levels
/// </summary>
public static class PermissionScopes
{
    private static readonly Dictionary<string, PermissionScope> Map = new()
    {
        // ==================== GLOBAL SCOPE ====================
        // Route requirements: NONE
        // These permissions do not require tenantId or projectId in route
        
        [PermissionCodes.TenantContextList] = PermissionScope.Global,
        [PermissionCodes.TenantContextAdminList] = PermissionScope.Global,
        
        
        // ==================== TENANT SCOPE ====================
        // Route requirements: tenantId
        // These permissions require tenantId in route
        
        // TENANT - BASE ACCESS
        [PermissionCodes.TenantView] = PermissionScope.Tenant,

        // TENANT - SETTINGS
        [PermissionCodes.TenantSettingsView] = PermissionScope.Tenant,
        [PermissionCodes.TenantSettingsEdit] = PermissionScope.Tenant,
        [PermissionCodes.TenantMembersManage] = PermissionScope.Tenant,
        [PermissionCodes.TenantProjectsCreate] = PermissionScope.Tenant,
        
        
        // ==================== PROJECT SCOPE ====================
        // Route requirements: tenantId, projectId
        // These permissions require BOTH tenantId and projectId in route
        
        // PROJECT - BASE ACCESS
        [PermissionCodes.ProjectView] = PermissionScope.Project,

        // PROJECT - MODULES
        [PermissionCodes.ProjectSettings] = PermissionScope.Project,
        [PermissionCodes.ProjectMembers] = PermissionScope.Project,
        [PermissionCodes.ProjectFiles] = PermissionScope.Project,
        [PermissionCodes.ProjectEstimates] = PermissionScope.Project,
        [PermissionCodes.ProjectCosts] = PermissionScope.Project,
        [PermissionCodes.ProjectSchedule] = PermissionScope.Project,
        [PermissionCodes.ProjectDashboardTracker] = PermissionScope.Project,

        // PROJECT - ADMIN ONLY
        [PermissionCodes.ProjectAdmin] = PermissionScope.Project,
    };

    /// <summary>
    /// Gets the scope for a given permission code
    /// </summary>
    /// <param name="permissionCode">The permission code to look up</param>
    /// <returns>The scope level, defaults to Tenant if not found</returns>
    public static PermissionScope Get(string permissionCode)
    {
        if (Map.TryGetValue(permissionCode, out var scope))
        {
            return scope;
        }
        
        // Default to Tenant scope for unknown permissions (safer than Global)
        return PermissionScope.Tenant;
    }
}
