using System;

namespace Business.Interfaces.WebModels.Tenants
{
    /// <summary>
    /// Tenant member details with role code instead of enum
    /// </summary>
    public record TenantMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string RoleCode,  // Changed from TenantRole enum
        bool IsActive,
        DateTime JoinedAt
    );
}
