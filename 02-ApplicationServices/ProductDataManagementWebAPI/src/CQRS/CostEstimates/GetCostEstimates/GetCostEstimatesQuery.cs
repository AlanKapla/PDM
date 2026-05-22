using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Query to get cost estimates based on scope (All, Mine, Shared).
    /// </summary>
    public sealed record GetCostEstimatesQuery : CostEstimateRequestBase, IRequestQuery<List<CostEstimateListItemWeb>>
    {
        public ResourceScope Scope { get; init; }

        public override string PermissionCode => Scope switch
        {
            ResourceScope.All => PermissionCodes.ProjectResourcesReadAll,
            ResourceScope.Mine => PermissionCodes.ProjectResourcesRead,
            ResourceScope.Shared => PermissionCodes.ProjectResourcesReadShared,
            _ => throw new ArgumentOutOfRangeException(nameof(Scope))
        };
    }
}
