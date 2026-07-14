using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using Microsoft.AspNetCore.Http;

namespace CQRS.AI.SubmitAICostImportBatch
{
    public sealed record SubmitAICostImportBatchCommand : IRequestCommand<AICostImportSubmitResultWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required IFormFileCollection Files { get; init; }
        public required CostDocumentType CostDocumentType { get; init; }
        public TrackedCostContextDto? TrackedCostContext { get; init; }

        public string PermissionCode => CostDocumentType == CostDocumentType.ProjectCost
            ? PermissionCodes.ProjectCosts
            : PermissionCodes.ProjectDashboardTracker;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
