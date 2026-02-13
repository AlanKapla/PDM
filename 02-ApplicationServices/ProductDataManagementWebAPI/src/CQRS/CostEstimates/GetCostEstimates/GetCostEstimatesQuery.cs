using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Query to get cost estimates based on scope (All, Mine, Shared)
    /// </summary>
    public sealed record GetCostEstimatesQuery(
        Guid TenantId,
        Guid ProjectId,
        ResourceScope Scope
    ) : IRequestQuery<List<CostEstimateListItemWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => Scope switch
        {
            ResourceScope.All => PermissionCodes.ProjectResourcesReadAll,
            ResourceScope.Mine => PermissionCodes.ProjectResourcesRead,
            ResourceScope.Shared => PermissionCodes.ProjectResourcesReadShared,
            _ => throw new ArgumentOutOfRangeException(nameof(Scope))
        };
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
