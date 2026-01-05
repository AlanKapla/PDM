namespace Business.Interfaces.Constants;

/// <summary>
/// Read-only fallback permissions granted to SuperAdmin when they are not a member of a tenant or project.
/// These permissions allow monitoring and auditing without modification capabilities.
/// </summary>
public static class SuperAdminFallbackPermissions
{
    /// <summary>
    /// Read-only permissions for tenants where SuperAdmin is not a member
    /// </summary>
    public static readonly HashSet<string> TenantReadOnly = new()
    {
        PermissionCodes.TenantListAvailable,
        PermissionCodes.TenantAdminListAvailable,
        PermissionCodes.RoleList,
        PermissionCodes.TenantView
    };

    /// <summary>
    /// Read-only permissions for projects where SuperAdmin is not a member
    /// </summary>
    public static readonly HashSet<string> ProjectReadOnly = new()
    {
        PermissionCodes.ProjectView,
        PermissionCodes.ProjectMembersView,
        PermissionCodes.ProjectResourcesReadAll
    };
}
