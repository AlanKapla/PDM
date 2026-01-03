using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetUserTenants
{
    public sealed record GetUserTenantsQuery : IRequestQuery<IEnumerable<UserTenantWeb>>;
}
