using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.CreateTenant
{
    public sealed record CreateTenantCommand : IRequestCommand<TenantDetailsWeb>
    {
        public required string Name { get; init; }
    }
}
