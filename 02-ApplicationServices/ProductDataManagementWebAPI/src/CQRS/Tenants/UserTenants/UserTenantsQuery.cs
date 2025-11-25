using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.UserTenants
{
    public sealed record UserTenantsQuery : IRequestQuery<IEnumerable<TenantDetailsWeb>>;
}
