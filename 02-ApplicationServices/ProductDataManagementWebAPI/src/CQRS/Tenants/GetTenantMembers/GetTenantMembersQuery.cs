using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetTenantMembers
{
    public record GetTenantMembersQuery(
        Guid TenantId
    ) : IRequestQuery<IEnumerable<TenantMemberWeb>>;
}
