using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public sealed record RemoveTenantInvitationCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid InvitationId { get; init; }

        public string PermissionCode => PermissionCodes.TenantMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
