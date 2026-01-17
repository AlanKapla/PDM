using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetAdminTenants
{
    public sealed record GetAdminTenantsQuery : IRequestQuery<IEnumerable<TenantBasicWeb>>;
}
