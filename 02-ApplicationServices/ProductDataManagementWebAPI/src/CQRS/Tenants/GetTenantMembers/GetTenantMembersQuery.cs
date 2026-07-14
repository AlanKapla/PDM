using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetTenantMembers
{
    public sealed record GetTenantMembersQuery : IRequestQuery<IEnumerable<TenantMemberWeb>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }

        public string PermissionCode => PermissionCodes.TenantSettingsView;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
