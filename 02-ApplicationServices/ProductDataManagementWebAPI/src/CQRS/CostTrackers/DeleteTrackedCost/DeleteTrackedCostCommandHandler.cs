using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    public sealed class DeleteTrackedCostCommandHandler
        : CostTrackerHandlerBase, IRequestHandler<DeleteTrackedCostCommand, Unit>
    {
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<DeleteTrackedCostCommandHandler> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public DeleteTrackedCostCommandHandler(
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<BaseCostAttachment> attachmentRepository,
            IBlobStorageService blobStorageService,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<DeleteTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, contractorService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.attachmentRepository = attachmentRepository;
            this.blobStorageService = blobStorageService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            DeleteTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

            TrackedCost cost = await GetAndValidateTrackedCostAsync(request.CostId, request.TenantId, request.ProjectId, cancellationToken);

            DateTime now = DateTime.UtcNow;
            await SoftDeleteAttachmentsAsync(cost.Id, now, cancellationToken);

            cost.IsDeleted = true;
            cost.DeletedAt = now;
            await trackedCostRepository.Update(cost);

            logger.LogInformation(
                "Deleted TrackedCost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            return Unit.Value;
        }

        private async Task SoftDeleteAttachmentsAsync(
            Guid costId, DateTime deletedAt, CancellationToken cancellationToken)
        {
            List<BaseCostAttachment> attachments = (await attachmentRepository.GetBySearch(
                a => a.CostId == costId)).ToList();

            foreach (BaseCostAttachment attachment in attachments)
            {
                attachment.IsDeleted = true;
                attachment.DeletedAt = deletedAt;
                await attachmentRepository.Update(attachment);

                try
                {
                    await blobStorageService.DeleteAsync(ContainerName, attachment.BlobName, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete blob {BlobName} for attachment {AttachmentId} during cost deletion",
                        attachment.BlobName, attachment.Id);
                }
            }
        }
    }
}
