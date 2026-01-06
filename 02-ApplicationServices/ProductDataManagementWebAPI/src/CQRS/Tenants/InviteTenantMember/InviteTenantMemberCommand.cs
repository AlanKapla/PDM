using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.InviteTenantMember
{
    public record InviteTenantMemberCommand(Guid TenantId, string Email) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantMembersManage;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
