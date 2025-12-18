using CQRS;
using MediatR;

namespace CQRS.Tenants.InviteTenantMember
{
    public record InviteTenantMemberCommand(Guid TenantId, string Email) : IRequestCommand<Unit>;
}
