using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostTrackers.Shared;
using Entities.Models;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    public sealed class DeleteTrackedCostCommandHandler
        : CostTrackerHandlerBase, IRequestHandler<DeleteTrackedCostCommand, Unit>
    {
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly IRepository<TrackedCostAttachment> attachmentRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<DeleteTrackedCostCommandHandler> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public DeleteTrackedCostCommandHandler(
            IReadRepository<TrackedCost> trackedCostRepository,
            IRepository<TrackedCostAttachment> attachmentRepository,
            IReadRepository<CostTracker> trackerRepository,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<DeleteTrackedCostCommandHandler> logger)
            : base(trackerRepository, currentUser, trackedCostRepository)
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
            TrackedCost cost = await GetAndValidateTrackedCostAsync(request.CostId, request.TenantId, request.ProjectId, cancellationToken);

            var now = DateTime.UtcNow;
            await SoftDeleteAttachmentsAsync(cost.Id, now, cancellationToken);

            cost.IsDeleted = true;
            cost.DeletedAt = now;
            await trackedCostRepository.Update(cost);

            logger.LogInformation(
                "Deleted TrackedCost {CostId} for tracker {TrackerId} by user {UserId}",
                cost.Id, cost.TrackerId, currentUser.Id);

            return Unit.Value;
        }

        private async Task SoftDeleteAttachmentsAsync(
            Guid costId, DateTime deletedAt, CancellationToken cancellationToken)
        {
            List<TrackedCostAttachment> attachments = (await attachmentRepository.GetBySearch(
                a => a.TrackedCostId == costId)).ToList();

            foreach (var attachment in attachments)
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
