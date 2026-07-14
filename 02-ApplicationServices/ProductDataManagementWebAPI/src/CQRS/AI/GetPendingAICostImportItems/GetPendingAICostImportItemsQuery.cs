using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.Shared;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.GetPendingAICostImportItems
{
    public sealed record GetPendingAICostImportItemsQuery : IRequestQuery<IReadOnlyList<AICostImportItemWeb>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class GetPendingAICostImportItemsQueryHandler
        : IRequestHandler<GetPendingAICostImportItemsQuery, IReadOnlyList<AICostImportItemWeb>>
    {
        private readonly IReadRepository<AICostImportItem> itemRepo;
        private readonly IReadRepository<AICostImportBatch> batchRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public GetPendingAICostImportItemsQueryHandler(
            IReadRepository<AICostImportItem> itemRepo,
            IReadRepository<AICostImportBatch> batchRepo,
            IAICostImportBlobService blobService,
            IAccessService accessService,
            ICurrentUser currentUser)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.blobService = blobService;
            this.accessService = accessService;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyList<AICostImportItemWeb>> Handle(
            GetPendingAICostImportItemsQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<AICostImportItem> items = await itemRepo.GetBySearch(
                i => i.TenantId == request.TenantId
                     && i.ProjectId == request.ProjectId
                     && (i.Status == AICostImportItemStatus.Pending
                         || i.Status == AICostImportItemStatus.ErrorNeedsReview
                         || i.Status == AICostImportItemStatus.DuplicateDetected));

            List<AICostImportItemWeb> result = new List<AICostImportItemWeb>();

            foreach (AICostImportItem item in items.OrderByDescending(i => i.CreatedAt))
            {
                AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                    b => b.Id == item.BatchId
                         && b.TenantId == request.TenantId
                         && b.ProjectId == request.ProjectId);

                if (batch is null)
                {
                    continue;
                }

                if (!await CanViewBatchAsync(batch, request, cancellationToken))
                {
                    continue;
                }

                result.Add(AICostImportMapper.MapItemToWeb(item, batch, blobService));
            }

            return result;
        }

        private async Task<bool> CanViewBatchAsync(
            AICostImportBatch batch,
            GetPendingAICostImportItemsQuery request,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            return await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: request.TenantId, ProjectId: request.ProjectId),
                cancellationToken: cancellationToken);
        }
    }
}
