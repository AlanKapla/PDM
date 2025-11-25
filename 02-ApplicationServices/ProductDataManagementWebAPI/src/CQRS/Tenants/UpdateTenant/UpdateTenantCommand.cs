using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed record UpdateTenantCommand(Guid TenantId, string Name) : IRequestCommand<TenantDetailsWeb>;
}
