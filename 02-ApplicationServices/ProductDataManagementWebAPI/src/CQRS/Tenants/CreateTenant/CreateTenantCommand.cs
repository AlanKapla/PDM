using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.CreateTenant
{
    public sealed record CreateTenantCommand(string Name) : IRequestCommand<TenantDetailsWeb>;
}