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
            ResourceScope.All => PermissionCodes.ProjectEstimates,
            ResourceScope.Mine => PermissionCodes.ProjectEstimates,
            ResourceScope.Shared => PermissionCodes.ProjectEstimates,
            _ => throw new ArgumentOutOfRangeException(nameof(Scope))
        };
    }
}
