using MediatR;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public record RemoveTenantInvitationCommand(Guid TenantId, Guid InvitationId) : IRequestCommand<Unit>;
}
