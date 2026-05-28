namespace Business.Interfaces.WebModels.Tenants
{
    /// <summary>
    /// Tenant member details
    /// </summary>
    public sealed record TenantMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        bool IsAdmin,
        bool IsActive,
        DateTime JoinedAt
    );
}
