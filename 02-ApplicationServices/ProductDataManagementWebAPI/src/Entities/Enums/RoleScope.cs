namespace Entities.Enums
{
    /// <summary>
    /// Defines the scope at which a role operates
    /// </summary>
    public enum RoleScope
    {
        /// <summary>
        /// System-level role (e.g., SYSTEM.SUPERADMIN)
        /// </summary>
        System = 0,
        
        /// <summary>
        /// Tenant-level role (e.g., TENANT.ADMIN, TENANT.MEMBER)
        /// </summary>
        Tenant = 1,
        
        /// <summary>
        /// Project-level role (e.g., PROJECT.ADMIN, PROJECT.EDITOR, PROJECT.COLLABORATOR, PROJECT.VIEWER)
        /// </summary>
        Project = 2
    }
}
