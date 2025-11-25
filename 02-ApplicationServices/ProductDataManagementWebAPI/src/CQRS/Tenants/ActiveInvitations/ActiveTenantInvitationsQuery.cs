using Business.Interfaces.WebModels.Tenants;
using MediatR;

namespace CQRS.Tenants.ActiveInvitations
{
    public record ActiveTenantInvitationsQuery() : IRequestQuery<IEnumerable<TenantInvitationWeb>>;
}
