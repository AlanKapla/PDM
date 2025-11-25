using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.ActiveTenant
{
    public sealed record ActiveTenantQuery : IRequestQuery<ActiveTenantWeb>;
}
