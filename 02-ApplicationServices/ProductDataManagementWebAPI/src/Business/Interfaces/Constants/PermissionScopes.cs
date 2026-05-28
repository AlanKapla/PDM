namespace Business.Interfaces.Constants;

/// <summary>
/// Maps permission codes to their required scope levels
/// </summary>
public static class PermissionScopes
{
    private static readonly IReadOnlyDictionary<string, PermissionScope> Map = new Dictionary<string, PermissionScope>
    {
        // ==================== GLOBAL SCOPE ====================
        // Route requirements: NONE
        // These permissions do not require tenantId or projectId in route
        
        [PermissionCodes.TenantContextList] = PermissionScope.Global,
        [PermissionCodes.TenantContextAdminList] = PermissionScope.Global,
        [PermissionCodes.RoleList] = PermissionScope.Global,
        
        
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
        
        
        // ==================== RESOURCE SCOPE ====================
        // Route requirements: tenantId, projectId, resourceId
        // These permissions require tenantId, projectId AND specific resourceId (fileId, costId, etc.)
        // Currently no permissions use Resource scope - all resource operations use Project scope
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
    
    /// <summary>
    /// Checks if a permission requires tenantId in the route
    /// </summary>
    public static bool RequiresTenantId(string permissionCode)
    {
        var scope = Get(permissionCode);
        return scope == PermissionScope.Tenant || scope == PermissionScope.Project || scope == PermissionScope.Resource;
    }
    
    /// <summary>
    /// Checks if a permission requires projectId in the route
    /// </summary>
    public static bool RequiresProjectId(string permissionCode)
    {
        var scope = Get(permissionCode);
        return scope == PermissionScope.Project || scope == PermissionScope.Resource;
    }
    
    /// <summary>
    /// Checks if a permission requires resourceId in the route
    /// </summary>
    public static bool RequiresResourceId(string permissionCode)
    {
        var scope = Get(permissionCode);
        return scope == PermissionScope.Resource;
    }
}
