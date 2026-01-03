namespace Business.Interfaces.Constants;

/// <summary>
/// Defines the scope level at which a permission operates
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// Global permission - does not require tenantId or projectId in route
    /// Examples: TENANT.LIST.AVAILABLE, system-wide operations
    /// Route requirements: NONE
    /// </summary>
    Global = 0,
    
    /// <summary>
    /// Tenant-scoped permission - requires tenantId in route
    /// Examples: TENANT.VIEW, TENANT.EDIT, TENANT.MEMBERS.MANAGE
    /// Route requirements: tenantId
    /// </summary>
    Tenant = 1,
    
    /// <summary>
    /// Project-scoped permission - requires BOTH tenantId and projectId in route
    /// Examples: PROJECT.VIEW, PROJECT.EDIT, PROJECT.RESOURCES.READ
    /// Route requirements: tenantId, projectId
    /// </summary>
    Project = 2,
    
    /// <summary>
    /// Resource-scoped permission - requires tenantId, projectId AND specific resourceId in route
    /// Examples: operations on specific files, costs, estimates
    /// Route requirements: tenantId, projectId, resourceId (fileId, costId, etc.)
    /// </summary>
    Resource = 3
}
