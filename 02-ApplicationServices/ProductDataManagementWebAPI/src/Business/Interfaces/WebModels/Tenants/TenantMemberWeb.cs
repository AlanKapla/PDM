namespace Business.Interfaces.WebModels.Tenants
{
    /// <summary>
    /// Tenant member details with role code instead of enum
    /// </summary>
    public sealed record TenantMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string RoleCode,
        bool IsActive,
        DateTime JoinedAt
    );
}
