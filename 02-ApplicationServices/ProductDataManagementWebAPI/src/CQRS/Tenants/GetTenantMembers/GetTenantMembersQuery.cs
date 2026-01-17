using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetTenantMembers
{
    public sealed record GetTenantMembersQuery(
        Guid TenantId
    ) : IRequestQuery<IEnumerable<TenantMemberWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
