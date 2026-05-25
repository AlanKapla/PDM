using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Behaviours;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed record GetTenantDetailsQuery : IRequestQuery<TenantDetailsWeb>, IAuthorizableRequest, IBypassSubscriptionCheck
    {
        public required Guid TenantId { get; init; }

        public string PermissionCode => PermissionCodes.TenantEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
