using Business.Interfaces.Constants;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public sealed record CopyCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<List<Guid>>
    {
        public List<Guid> TargetProjectIds { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
