using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.RejectAICostImportItem
{
    public sealed record RejectAICostImportItemCommand : IRequestCommand<MediatR.Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid ItemId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public sealed class RejectAICostImportItemCommandHandler
        : IRequestHandler<RejectAICostImportItemCommand, MediatR.Unit>
    {
        private readonly IRepository<AICostImportItem> itemRepo;
        private readonly IRepository<AICostImportBatch> batchRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<RejectAICostImportItemCommandHandler> logger;

        public RejectAICostImportItemCommandHandler(
            IRepository<AICostImportItem> itemRepo,
            IRepository<AICostImportBatch> batchRepo,
            IAICostImportBlobService blobService,
            IAccessService accessService,
            ICurrentUser currentUser,
            ILogger<RejectAICostImportItemCommandHandler> logger)
        {
            this.itemRepo = itemRepo;
            this.batchRepo = batchRepo;
            this.blobService = blobService;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<MediatR.Unit> Handle(
            RejectAICostImportItemCommand request,
            CancellationToken cancellationToken)
        {
            AICostImportItem? item = await itemRepo.GetFirstBySearch(
                i => i.Id == request.ItemId
                     && i.TenantId == request.TenantId
                     && i.ProjectId == request.ProjectId);

            if (item is null)
            {
                throw new NotFoundApiException(nameof(AICostImportItem), request.ItemId.ToString());
            }

            if (item.Status is not (
                AICostImportItemStatus.Pending
                or AICostImportItemStatus.ErrorNeedsReview
                or AICostImportItemStatus.DuplicateDetected))
            {
                throw new ConflictApiException(
                    nameof(AICostImportItem),
                    request.ItemId.ToString(),
                    "Only pending or error items can be rejected.");
            }

            AICostImportBatch? batch = await batchRepo.GetFirstBySearch(
                b => b.Id == item.BatchId
                     && b.TenantId == request.TenantId
                     && b.ProjectId == request.ProjectId);

            if (batch is null)
            {
                throw new NotFoundApiException(nameof(AICostImportBatch), item.BatchId.ToString());
            }

            await EnsureBatchAccessAsync(batch, request.TenantId, request.ProjectId, cancellationToken);

            try
            {
                await blobService.DeletePendingAsync(item.BlobPath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete blob for rejected item {ItemId}", item.Id);
            }

            if (item.Status == AICostImportItemStatus.Pending)
            {
                batch.PendingCount = Math.Max(0, batch.PendingCount - 1);
            }
            else if (item.Status == AICostImportItemStatus.ErrorNeedsReview)
            {
                batch.ErrorCount = Math.Max(0, batch.ErrorCount - 1);
            }
            else if (item.Status == AICostImportItemStatus.DuplicateDetected)
            {
                batch.DuplicateCount = Math.Max(0, batch.DuplicateCount - 1);
            }

            await batchRepo.Update(batch);
            await itemRepo.Delete(item);
            await itemRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Rejected AI cost import item {ItemId}", item.Id);
            return MediatR.Unit.Value;
        }

        private async Task EnsureBatchAccessAsync(
            AICostImportBatch batch,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            string permission = batch.CostDocumentType == Entities.Enums.CostDocumentType.ProjectCost
                ? PermissionCodes.ProjectCosts
                : PermissionCodes.ProjectDashboardTracker;

            bool authorized = await accessService.AuthorizeAsync(
                currentUser,
                permission,
                new ResourceRef(TenantId: tenantId, ProjectId: projectId),
                cancellationToken: cancellationToken);

            if (!authorized)
            {
                throw new ForbiddenApiException("You do not have permission to reject this import item.");
            }
        }
    }
}
