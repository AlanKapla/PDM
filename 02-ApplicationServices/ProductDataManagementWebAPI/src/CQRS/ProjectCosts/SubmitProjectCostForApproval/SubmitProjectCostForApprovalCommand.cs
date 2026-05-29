using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed record SubmitProjectCostForApprovalCommand : IRequestCommand<ProjectCostListItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid CostId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectCosts;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
