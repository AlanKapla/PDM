using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed record UpdateTenantCommand : IRequestCommand<TenantDetailsWeb>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public required string Name { get; init; }

        public string PermissionCode => PermissionCodes.TenantSettingsEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
