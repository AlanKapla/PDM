using Entities.Enums;

namespace Business.Interfaces.WebModels.Users
{
    /// <summary>
    /// User details with permissions for UI authorization
    /// </summary>
    public sealed record UserDetailsWeb(
        Guid Id, 
        string FirstName, 
        string LastName, 
        string Email, 
        Guid? ActiveTenantId,
        
        /// <summary>
        /// Permissions in the active tenant (empty if no active tenant)
        /// </summary>
        HashSet<string> ActiveTenantPermissions,
        
        /// <summary>
        /// Project role codes mapped by projectId (e.g., "PROJECT.ADMIN", "PROJECT.EDITOR")
        /// </summary>
        Dictionary<Guid, string> ProjectRoleCodes,
        
        /// <summary>
        /// Permissions in each project mapped by projectId
        /// </summary>
        Dictionary<Guid, HashSet<string>> ProjectPermissions
    );
}
