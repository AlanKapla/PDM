using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed record GetTenantDetailsQuery : IRequestQuery<TenantDetailsWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }

        public string PermissionCode => PermissionCodes.TenantSettingsEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
