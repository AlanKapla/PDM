using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed record GetTenantDetailsQuery(
        Guid TenantId
    ) : IRequestQuery<TenantDetailsWeb>;
}
