using Entities.Enums;

namespace Business.Interfaces.WebModels.Users
{
    /// <summary>
    /// User details with tenant-level permissions for UI authorization
    /// Project-specific permissions are now returned by ProjectDetailsWeb
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

        bool IsSuperAdmin,

        string? PhoneNumber,
        string? CompanyName,
        string? TaxId,
        string? Street,
        string? City,
        string? PostalCode,
        string? Country
    );
}
