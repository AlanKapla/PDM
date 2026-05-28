using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Contractors;

namespace CQRS.Contractors.GetContractor
{
    public sealed record GetContractorQuery : IRequestQuery<ContractorWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ContractorId { get; init; }

        public string PermissionCode => PermissionCodes.TenantSettingsView;
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
