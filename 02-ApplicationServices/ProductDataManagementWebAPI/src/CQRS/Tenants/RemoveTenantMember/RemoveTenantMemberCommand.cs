using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.RemoveTenantMember
{
    public record RemoveTenantMemberCommand(Guid TenantId, Guid UserId) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantMembersManage;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
