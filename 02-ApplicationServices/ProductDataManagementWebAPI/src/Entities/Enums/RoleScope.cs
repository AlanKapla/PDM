namespace Entities.Enums
{
    /// <summary>
    /// Defines the scope at which a role operates
    /// </summary>
    public enum RoleScope
    {
        /// <summary>
        /// Tenant-level role (e.g., TENANT.ADMIN, TENANT.MEMBER)
        /// </summary>
        Tenant = 0,
        
        /// <summary>
        /// Project-level role (e.g., PROJECT.ADMIN, PROJECT.EDITOR, PROJECT.COLLABORATOR, PROJECT.VIEWER)
        /// </summary>
        Project = 1
    }
}
