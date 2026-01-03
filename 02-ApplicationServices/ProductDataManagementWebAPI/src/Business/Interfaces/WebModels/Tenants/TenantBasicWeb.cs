namespace Business.Interfaces.WebModels.Tenants
{
    public sealed record TenantBasicWeb(
        Guid Id,
        string Name,
        DateTime CreatedAt,
        bool IsActive
    );
}
