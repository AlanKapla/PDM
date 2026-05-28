namespace Business.Interfaces.Constants;

public static class SuperAdminFallbackPermissions
{
    public static readonly HashSet<string> TenantReadOnly = new()
    {
        PermissionCodes.TenantContextList,
        PermissionCodes.TenantContextAdminList,
        PermissionCodes.RoleList,
        PermissionCodes.TenantView,
        PermissionCodes.TenantSettingsView
    };

    public static readonly HashSet<string> ProjectReadOnly = new()
    {
        PermissionCodes.ProjectView,
        PermissionCodes.ProjectSettings,
        PermissionCodes.ProjectMembers,
        PermissionCodes.ProjectFiles,
        PermissionCodes.ProjectEstimates,
        PermissionCodes.ProjectCosts,
        PermissionCodes.ProjectSchedule,
        PermissionCodes.ProjectDashboardTracker
    };
}
