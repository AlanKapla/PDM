using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed record UpdateTenantCommand : IRequestCommand<TenantDetailsWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required string Name { get; init; }

        public string PermissionCode => PermissionCodes.TenantEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
