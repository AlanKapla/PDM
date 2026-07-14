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
        /// Whether the user is admin in the active tenant. False if no active tenant.
        /// </summary>
        bool IsActiveTenantAdmin,

        /// <summary>
        /// Whether the user has the SYSTEM.SUPERADMIN role.
        /// </summary>
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
