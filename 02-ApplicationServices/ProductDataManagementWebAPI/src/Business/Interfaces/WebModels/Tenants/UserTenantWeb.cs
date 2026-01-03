namespace Business.Interfaces.WebModels.Tenants
{
    public sealed record UserTenantWeb(
        Guid Id,
        string Name,
        DateTime CreatedAt,
        bool IsActive,
        string RoleCode,
        bool IsActiveTenant
    );
}
