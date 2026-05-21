using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Contractors;

namespace CQRS.Contractors.GetContractors
{
    public sealed record GetContractorsQuery : IRequestQuery<IEnumerable<ContractorWeb>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public string? Search { get; init; }

        public string PermissionCode => PermissionCodes.TenantView;
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
