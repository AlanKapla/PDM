using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
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
        private readonly IRepository<ProjectCost> projectCostRepository;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<DeleteTrackedCostCommandHandler> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public DeleteTrackedCostCommandHandler(
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<ProjectCost> projectCostRepository,
            IRepository<BaseCostAttachment> attachmentRepository,
            IBlobStorageService blobStorageService,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<DeleteTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, contractorService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.projectCostRepository = projectCostRepository;
            this.attachmentRepository = attachmentRepository;
            this.blobStorageService = blobStorageService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            DeleteTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

            BaseCost cost = await ResolveEditableCostAsync(
                request.CostId, request.TenantId, request.ProjectId, cancellationToken);

            DateTime now = DateTime.UtcNow;
            await SoftDeleteAttachmentsAsync(cost.Id, now, cancellationToken);

            cost.IsDeleted = true;
            cost.DeletedAt = now;
            await PersistCostAsync(cost);

            logger.LogInformation(
                "Deleted cost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            return Unit.Value;
        }

        private async Task<BaseCost> ResolveEditableCostAsync(
            Guid costId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            TrackedCost? trackedCost = await trackedCostRepository.GetFirstBySearch(
                tc => tc.Id == costId && tc.TenantId == tenantId && tc.ProjectId == projectId);

            if (trackedCost is not null)
            {
                return trackedCost;
            }

            ProjectCost? projectCost = await projectCostRepository.GetFirstBySearch(
                pc => pc.Id == costId && pc.TenantId == tenantId && pc.ProjectId == projectId
                      && pc.ApprovalStatus == CostApprovalStatus.Approved);

            if (projectCost is not null)
            {
                return projectCost;
            }

            throw new NotFoundApiException(nameof(TrackedCost), costId.ToString());
        }

        private async Task PersistCostAsync(BaseCost cost)
        {
            if (cost is TrackedCost trackedCost)
            {
                await trackedCostRepository.Update(trackedCost);
                return;
            }

            if (cost is ProjectCost projectCost)
            {
                await projectCostRepository.Update(projectCost);
            }
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
