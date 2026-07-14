using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.GetPendingAICostImportCount
{
    public sealed record GetPendingAICostImportCountQuery : IRequestQuery<PendingAICostImportCountWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class GetPendingAICostImportCountQueryHandler
        : IRequestHandler<GetPendingAICostImportCountQuery, PendingAICostImportCountWeb>
    {
        private readonly IReadRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public GetPendingAICostImportCountQueryHandler(
            IReadRepository<AICostImportItem> itemRepo,
            IReadRepository<AICostImportBatch> batchRepo,
            IAccessService accessService,
            ICurrentUser currentUser)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.accessService = accessService;
            this.currentUser = currentUser;
        }

        public async Task<PendingAICostImportCountWeb> Handle(
            GetPendingAICostImportCountQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<AICostImportItem> items = await itemRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.ProjectId == request.ProjectId
                     && (i.Status == AICostImportItemStatus.Pending
                         || i.Status == AICostImportItemStatus.ErrorNeedsReview
                         || i.Status == AICostImportItemStatus.DuplicateDetected));

            int pendingCount = 0;
            int errorCount = 0;
            int duplicateCount = 0;

            foreach (AICostImportItem item in items)
            {
                AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                    b => b.Id == item.BatchId
                         && b.TenantId == request.TenantId
                         && b.ProjectId == request.ProjectId);

                if (batch is null)
                {
                    continue;
                }

                string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                    ? PermissionCodes.ProjectCosts
                    : PermissionCodes.ProjectDashboardTracker;

                bool authorized = await accessService.AuthorizeAsync(
                    currentUser,
                    permission,
                    new ResourceRef(TenantId: request.TenantId, ProjectId: request.ProjectId),
                    cancellationToken: cancellationToken);

                if (!authorized)
                {
                    continue;
                }

                if (item.Status == AICostImportItemStatus.Pending)
                {
                    pendingCount++;
                }
                else if (item.Status == AICostImportItemStatus.ErrorNeedsReview)
                {
                    errorCount++;
                }
                else if (item.Status == AICostImportItemStatus.DuplicateDetected)
                {
                    duplicateCount++;
                }
            }

            return new PendingAICostImportCountWeb
            {
                PendingCount = pendingCount,
                ErrorCount = errorCount,
                DuplicateCount = duplicateCount
            };
        }
    }
}
