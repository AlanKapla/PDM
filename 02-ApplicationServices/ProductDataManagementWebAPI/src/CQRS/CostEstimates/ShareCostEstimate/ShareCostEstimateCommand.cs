using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.ShareCostEstimate
{
    public sealed record ShareCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public List<Guid> ShareWithUserIds { get; init; } = [];

        public override string PermissionCode => PermissionCodes.ProjectResourcesShare;
    }
}
