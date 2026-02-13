using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    public class UpdateProjectCostCommandHandler : IRequestHandler<UpdateProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IReadRepository<SharedProjectCost> sharedProjectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<UpdateProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify cost exists and belongs to the correct project/tenant
            var projectCost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted)

                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR cost owner OR user with share access
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = projectCost.UserId == currentUser.Id;
            
            bool hasShareAccess = false;
            if (!isAdmin && !isCostOwner)
            {
                var share = await sharedProjectCostRepo.GetFirstBySearch(
                    spc => spc.ProjectCostId == request.CostId 
                        && spc.SharedWithUserId == currentUser.Id);
                
                hasShareAccess = share != null;
                
                if (!hasShareAccess)
                {
                    throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
                }
            }

            // 3. Determine edit permissions
            bool canEditAllFields = isAdmin || isCostOwner;
            bool canEditOnlyIsClosed = hasShareAccess && !canEditAllFields;

            // 4. Validate edit permissions for shared user
            if (canEditOnlyIsClosed)
            {
                // Only update IsClosed for shared users
                projectCost.IsClosed = request.IsClosed;
                projectCost.UpdatedAt = DateTime.UtcNow;

                await projectCostRepo.Update(projectCost);
                await projectCostRepo.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Cost {CostId} IsClosed updated to {IsClosed} in project {ProjectId} by shared user {UserId}",
                    request.CostId, request.IsClosed, request.ProjectId, currentUser.Id);

                return Unit.Value;
            }

            // 5. Full update for admin or owner
            var (grossAmount, netAmount, vatRate) = AmountCalculationHelper.CalculateAmounts(
                request.NetAmount, 
                request.VatRate, 
                request.GrossAmount);

            projectCost.Name = request.Name;
            projectCost.Place = request.Place;
            projectCost.Date = request.Date.Date;
            projectCost.Description = request.Description;
            projectCost.NetAmount = netAmount;
            projectCost.VatRate = vatRate;
            projectCost.GrossAmount = grossAmount;
            projectCost.IsClosed = request.IsClosed;
            projectCost.UpdatedAt = DateTime.UtcNow;

            // 6. Handle document removal
            if (request.RemoveDocument && projectCost.HasDocument && !string.IsNullOrWhiteSpace(projectCost.DocumentBlobPath))
            {
                try
                {
                    string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
                    await blobStorageService.DeleteAsync(containerName, projectCost.DocumentBlobPath, cancellationToken);

                    projectCost.HasDocument = false;
                    projectCost.DocumentFileName = null;
                    projectCost.DocumentBlobPath = null;
                    projectCost.DocumentContentType = null;
                    projectCost.DocumentSizeBytes = null;

                    logger.LogInformation(
                        "Document removed for cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete document for cost {CostId}, continuing with metadata removal",
                        request.CostId);
                    
                    projectCost.HasDocument = false;
                    projectCost.DocumentFileName = null;
                    projectCost.DocumentBlobPath = null;
                    projectCost.DocumentContentType = null;
                    projectCost.DocumentSizeBytes = null;
                }
            }

            // 7. Handle new document upload
            if (request.Document != null)
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
                    string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
                    string fileExtension = Path.GetExtension(request.Document.FileName).ToLowerInvariant();
                    string blobFileName = $"{request.CostId}{fileExtension}";
                    string blobPath = $"{request.TenantId}/{request.ProjectId}/{currentUser.Id}/{request.CostId}/{blobFileName}";

                    using (Stream stream = request.Document.OpenReadStream())
                    {
                        await blobStorageService.UploadAsync(
                            containerName,
                            blobPath,
                            stream,
                            request.Document.ContentType,
                            cancellationToken);
                    }

                    projectCost.HasDocument = true;
                    projectCost.DocumentFileName = request.Document.FileName;
                    projectCost.DocumentBlobPath = blobPath;
                    projectCost.DocumentContentType = request.Document.ContentType;
                    projectCost.DocumentSizeBytes = request.Document.Length;

                    logger.LogInformation(
                        "New document uploaded for cost {CostId} in project {ProjectId}",
                        request.CostId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upload new document for cost {CostId}",
                        request.CostId);
                    
                    throw new ValidationApiException("Cost updated but document upload failed");
                }
            }

            await projectCostRepo.Update(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} fully updated in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
