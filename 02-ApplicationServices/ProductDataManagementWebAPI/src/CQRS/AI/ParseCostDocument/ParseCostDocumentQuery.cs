using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.AI;
using Microsoft.AspNetCore.Http;

namespace CQRS.AI.ParseCostDocument
{
    public sealed record ParseCostDocumentQuery : IRequestQuery<ParsedCostDto>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required IFormFile File { get; init; }
        public CostDocumentType CostType { get; init; } = CostDocumentType.TrackedCost;

        public string PermissionCode => CostType == CostDocumentType.ProjectCost
            ? PermissionCodes.ProjectCosts
            : PermissionCodes.ProjectDashboardTracker;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
