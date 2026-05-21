using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.ActiveInvitations
{
    public sealed record ActiveTenantInvitationsQuery : IRequestQuery<IEnumerable<TenantInvitationWeb>>;
}
