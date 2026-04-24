using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.Shared
{
    public abstract class ProjectCostHandlerBase
    {
        private readonly IBlobStorageService blobStorageService;

        protected ProjectCostHandlerBase(IBlobStorageService blobStorageService)
        {
            this.blobStorageService = blobStorageService;
        }

        protected async Task UploadDocumentToCostAsync(
            ProjectCost projectCost,
            IFormFile document,
            Guid tenantId,
            Guid costId,
            CancellationToken cancellationToken)
        {
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
            string fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();
            string blobFileName = $"{costId}{fileExtension}";
            string blobPath = $"{tenantId}/{projectCost.ProjectId}/{projectCost.UserId}/{costId}/{blobFileName}";

            using (Stream stream = document.OpenReadStream())
            {
                await blobStorageService.UploadAsync(containerName, blobPath, stream, document.ContentType, cancellationToken);
            }

            projectCost.HasDocument = true;
            projectCost.DocumentFileName = document.FileName;
            projectCost.DocumentBlobPath = blobPath;
            projectCost.DocumentContentType = document.ContentType;
                    projectCost.DocumentSizeBytes = document.Length;
                    }
                }
            }
