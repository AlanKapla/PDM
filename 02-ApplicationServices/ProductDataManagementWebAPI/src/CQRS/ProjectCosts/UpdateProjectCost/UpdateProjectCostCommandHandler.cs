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
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateProjectCostCommandHandler> logger;

        public UpdateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<UpdateProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateProjectCostCommand request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership

            // Get existing cost
            var projectCost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted);

            if (projectCost == null)
            {
                throw new NotFoundApiException("ProjectCost", request.CostId.ToString());
            }

            // Verify ownership - only the user who created the cost can update it
            if (projectCost.UserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Only the cost owner can update it");
            }

            // Calculate amounts using helper
            var (grossAmount, netAmount, vatRate) = AmountCalculationHelper.CalculateAmounts(
                request.NetAmount, 
                request.VatRate, 
                request.GrossAmount);

            // Update basic fields
            projectCost.Name = request.Name;
            projectCost.Place = request.Place;
            projectCost.Date = request.Date.Date;
            projectCost.Description = request.Description;
            projectCost.NetAmount = netAmount;
            projectCost.VatRate = vatRate;
            projectCost.GrossAmount = grossAmount;
            projectCost.IsClosed = request.IsClosed;
            projectCost.UpdatedAt = DateTime.UtcNow;

            // Handle document removal
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
                    
                    // Clear metadata even if blob deletion fails
                    projectCost.HasDocument = false;
                    projectCost.DocumentFileName = null;
                    projectCost.DocumentBlobPath = null;
                    projectCost.DocumentContentType = null;
                    projectCost.DocumentSizeBytes = null;
                }
            }

            // Handle new document upload
            if (request.Document != null)
            {
                // Delete old document if exists
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

                // Upload new document
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
                "Cost {CostId} updated in project {ProjectId} by user {UserId}",
                request.CostId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
