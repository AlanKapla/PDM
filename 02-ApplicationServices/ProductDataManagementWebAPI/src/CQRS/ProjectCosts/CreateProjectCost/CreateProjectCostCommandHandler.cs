using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public class CreateProjectCostCommandHandler : IRequestHandler<CreateProjectCostCommand, Guid>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreateProjectCostCommandHandler> logger;

        public CreateProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<CreateProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Guid> Handle(CreateProjectCostCommand request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership

            // Calculate amounts using helper
            var (grossAmount, netAmount, vatRate) = AmountCalculationHelper.CalculateAmounts(
                request.NetAmount, 
                request.VatRate, 
                request.GrossAmount);

            // Create ProjectCost entity
            var projectCost = new ProjectCost
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                Name = request.Name,
                Place = request.Place,
                Date = request.Date.Date,
                Description = request.Description,
                NetAmount = netAmount,
                VatRate = vatRate,
                GrossAmount = grossAmount,
                IsClosed = request.IsClosed,
                HasDocument = request.Document != null,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await projectCostRepo.Insert(projectCost);
            await projectCostRepo.SaveChangesAsync(cancellationToken);

            Guid costId = projectCost.Id;

            // Upload document if provided
            if (request.Document != null)
            {
                try
                {
                    string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
                    string fileExtension = Path.GetExtension(request.Document.FileName).ToLowerInvariant();
                    string blobFileName = $"{costId}{fileExtension}";
                    string blobPath = $"{request.TenantId}/{request.ProjectId}/{currentUser.Id}/{costId}/{blobFileName}";

                    using (Stream stream = request.Document.OpenReadStream())
                    {
                        await blobStorageService.UploadAsync(
                            containerName,
                            blobPath,
                            stream,
                            request.Document.ContentType,
                            cancellationToken);
                    }

                    // Update entity with document info
                    projectCost.DocumentFileName = request.Document.FileName;
                    projectCost.DocumentBlobPath = blobPath;
                    projectCost.DocumentContentType = request.Document.ContentType;
                    projectCost.DocumentSizeBytes = request.Document.Length;

                    await projectCostRepo.Update(projectCost);
                    await projectCostRepo.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Document uploaded for cost {CostId} in project {ProjectId}",
                        costId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upload document for cost {CostId} in project {ProjectId}",
                        costId, request.ProjectId);
                    
                    // Document upload failed, but cost is created
                    // Mark HasDocument as false
                    projectCost.HasDocument = false;
                    await projectCostRepo.Update(projectCost);
                    await projectCostRepo.SaveChangesAsync(cancellationToken);
                    
                    throw new ValidationApiException("Cost created but document upload failed");
                }
            }

            logger.LogInformation(
                "Cost {CostId} created in project {ProjectId} by user {UserId}",
                costId, request.ProjectId, currentUser.Id);

            return costId;
        }
    }
}
