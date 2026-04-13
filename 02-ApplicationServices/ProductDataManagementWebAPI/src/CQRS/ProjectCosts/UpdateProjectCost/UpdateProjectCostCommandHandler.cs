using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.Shared;
using Entities.Models;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    public class UpdateProjectCostCommandHandler : ProjectCostHandlerBase, IRequestHandler<UpdateProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IReadRepository<SharedProjectCost> sharedProjectCostRepo,
            IReadRepository<CostTracker> costTrackerRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<ProjectCostTrackedCostLink> projectCostLinkRepository,
            IRepository<TrackedCostAttachment> attachmentRepository,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<UpdateProjectCostCommandHandler> logger)
            : base(costTrackerRepository, trackedCostRepository, projectCostLinkRepository, blobStorageService, attachmentRepository)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            ProjectCost projectCost = await GetAndValidateProjectCostAsync(request, cancellationToken);
            bool canEditOnlyIsClosed = await ValidateAccessAndGetPermissionsAsync(request, projectCost, cancellationToken);

            if (canEditOnlyIsClosed)
            {
                await HandleSharedUserUpdateAsync(request, projectCost, cancellationToken);
                return Unit.Value;
            }

            bool wasAccepted = projectCost.IsClosed;
            ApplyFieldUpdates(request, projectCost);
            await HandleDocumentOperationsAsync(request, projectCost, cancellationToken);

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            await HandleTrackerOperationsAsync(request, projectCost, wasAccepted, cancellationToken);

            logger.LogInformation(
                "Cost {CostId} fully updated in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }

        private async Task<ProjectCost> GetAndValidateProjectCostAsync(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            return await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId
                    && pc.TenantId == request.TenantId
                    && pc.ProjectId == request.ProjectId
                    && !pc.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
        }

        private async Task<bool> ValidateAccessAndGetPermissionsAsync(UpdateProjectCostCommand request, ProjectCost projectCost, CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = projectCost.UserId == currentUser.Id;
            bool hasShareAccess = false;

            if (!isAdmin && !isCostOwner)
            {
                SharedProjectCost? share = await sharedProjectCostRepo.GetFirstBySearch(
                    spc => spc.ProjectCostId == request.CostId
                        && spc.SharedWithUserId == currentUser.Id);

                hasShareAccess = share != null;

                if (!hasShareAccess)
                {
                    throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
                }
            }

            bool canEditAllFields = isAdmin || isCostOwner;
            return hasShareAccess && !canEditAllFields;
        }

        private async Task HandleSharedUserUpdateAsync(UpdateProjectCostCommand request, ProjectCost projectCost, CancellationToken cancellationToken)
        {
            bool wasAccepted = projectCost.IsClosed;
            projectCost.IsClosed = request.IsClosed;
            projectCost.UpdatedAt = DateTime.UtcNow;

            await projectCostRepo.Update(projectCost);

            if (!wasAccepted && request.IsClosed)
            {
                await CreateTrackerLinkAsync(projectCost, request.TenantId, request.ProjectId, cancellationToken);
            }
            else if (wasAccepted && !request.IsClosed)
            {
                await RemoveTrackerLinkAsync(projectCost.Id, cancellationToken);
            }

            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} IsClosed updated to {IsClosed} in project {ProjectId} by shared user {UserId}",
                request.CostId, request.IsClosed, request.ProjectId, currentUser.Id);
        }

        private void ApplyFieldUpdates(UpdateProjectCostCommand request, ProjectCost projectCost)
        {
            projectCost.Name = request.Name;
            projectCost.Place = request.Place;
            projectCost.Date = request.Date.Date;
            projectCost.Description = request.Description;
            projectCost.NetAmount = request.NetAmount;
            projectCost.GrossAmount = request.GrossAmount ?? request.NetAmount!.Value;
            projectCost.IsClosed = request.IsClosed;
            projectCost.UpdatedAt = DateTime.UtcNow;
        }

        private async Task HandleDocumentOperationsAsync(UpdateProjectCostCommand request, ProjectCost projectCost, CancellationToken cancellationToken)
        {
            if (request.RemoveDocument && projectCost.HasDocument && !string.IsNullOrWhiteSpace(projectCost.DocumentBlobPath))
            {
                try
                {
                    string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
                    await blobStorageService.DeleteAsync(containerName, projectCost.DocumentBlobPath, cancellationToken);

                    logger.LogInformation(
                        "Document removed for cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete document for cost {CostId}, continuing with metadata removal",
                        request.CostId);
                }

                projectCost.HasDocument = false;
                projectCost.DocumentFileName = null;
                projectCost.DocumentBlobPath = null;
                projectCost.DocumentContentType = null;
                projectCost.DocumentSizeBytes = null;
            }

            if (request.UpdatedDocument != null)
            {
                if (projectCost.HasDocument && !string.IsNullOrWhiteSpace(projectCost.DocumentBlobPath))
                {
                    try
                    {
                        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
                        await blobStorageService.DeleteAsync(containerName, projectCost.DocumentBlobPath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to delete old document for cost {CostId}",
                            request.CostId);
                    }
                }

                try
                {
                    await UploadDocumentToCostAsync(projectCost, request.UpdatedDocument, request.TenantId, request.CostId, cancellationToken);

                    logger.LogInformation(
                        "Document replaced for cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upload updated document for cost {CostId}",
                        request.CostId);

                    throw new ValidationApiException("Cost updated but document upload failed");
                }
            }
            else if (request.Document != null)
            {
                try
                {
                    await UploadDocumentToCostAsync(projectCost, request.Document, request.TenantId, request.CostId, cancellationToken);

                    logger.LogInformation(
                        "Document added to cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upload document for cost {CostId}",
                        request.CostId);

                    throw new ValidationApiException("Cost updated but document upload failed");
                }
            }
        }

        private async Task HandleTrackerOperationsAsync(UpdateProjectCostCommand request, ProjectCost projectCost, bool wasAccepted, CancellationToken cancellationToken)
        {
            if (!wasAccepted && request.IsClosed)
            {
                await CreateTrackerLinkAsync(projectCost, request.TenantId, request.ProjectId, cancellationToken);
                await projectCostRepo.SaveChangesAsync(cancellationToken);
            }
            else if (wasAccepted && !request.IsClosed)
            {
                await RemoveTrackerLinkAsync(projectCost.Id, cancellationToken);
                await projectCostRepo.SaveChangesAsync(cancellationToken);
            }
            else if (wasAccepted && request.IsClosed)
            {
                await SyncTrackerCostAsync(projectCost, cancellationToken);
                await projectCostRepo.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
