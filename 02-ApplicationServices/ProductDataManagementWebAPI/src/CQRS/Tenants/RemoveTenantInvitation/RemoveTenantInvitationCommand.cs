using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public record RemoveTenantInvitationCommand(Guid TenantId, Guid InvitationId) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantMembersManage;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
