using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów kosztorysu
    /// </summary>
    public sealed record GetCostEstimateDetailsQuery(
        Guid CostEstimateId
    ) : IRequestQuery<CostEstimateDetailsWeb>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
