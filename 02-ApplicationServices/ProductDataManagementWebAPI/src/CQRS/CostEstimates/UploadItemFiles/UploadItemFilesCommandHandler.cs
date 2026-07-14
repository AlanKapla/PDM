using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UploadItemFiles
{
    public sealed class UploadItemFilesCommandHandler : IRequestHandler<UploadItemFilesCommand, List<Guid>>
    {
        private readonly IRepository<CostEstimateItemFile> itemFileRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadItemFilesCommandHandler> logger;

        public UploadItemFilesCommandHandler(
            IRepository<CostEstimateItemFile> itemFileRepo,
            IBlobStorageService blobStorageService,
            ICostEstimateCacheService ceCacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser,
            ILogger<UploadItemFilesCommandHandler> logger)
        {
            this.itemFileRepo = itemFileRepo;
            this.blobStorageService = blobStorageService;
            this.ceCacheService = ceCacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<List<Guid>> Handle(UploadItemFilesCommand request, CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request, cancellationToken);
            await ValidateItemExistsAsync(request, cancellationToken);

            if (request.Files.Count == 0)
            {
                return new List<Guid>();
            }

            List<Guid> createdFileIds = await UploadNewFilesAsync(request, cancellationToken);

            await InvalidateCacheAsync(request, cancellationToken);

            return createdFileIds;
        }

        private async Task ValidateAccessAsync(UploadItemFilesCommand request, CancellationToken cancellationToken)
        {
            CostEstimate? costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (costEstimate is null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
            {
                throw new ForbiddenApiException("Read-only access does not allow file uploads.");
            }
        }

        private async Task ValidateItemExistsAsync(UploadItemFilesCommand request, CancellationToken cancellationToken)
        {
            Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }
        }

        private async Task<List<Guid>> UploadNewFilesAsync(UploadItemFilesCommand request, CancellationToken cancellationToken)
        {
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);
            List<CostEstimateItemFile> itemFiles = new List<CostEstimateItemFile>();
            List<Guid> createdFileIds = new List<Guid>();

            for (int i = 0; i < request.Files.Count; i++)
            {
                IFormFile file = request.Files[i];
                string fileExtension = Path.GetExtension(file.FileName);

                CostEstimateItemFile itemFile = BuildItemFile(request, file, i);
                string blobName = BuildBlobName(request, itemFile.Id, fileExtension);
                itemFile.BlobName = blobName;

                using (Stream stream = file.OpenReadStream())
                {
                    await blobStorageService.UploadAsync(
                        containerName,
                        blobName,
                        stream,
                        file.ContentType,
                        cancellationToken);
                }

                itemFiles.Add(itemFile);
                createdFileIds.Add(itemFile.Id);

                logger.LogInformation(
                    "Uploaded item file {FileName} (ID: {FileId}) for item {ItemId} in cost estimate {CostEstimateId}",
                    file.FileName, itemFile.Id, request.ItemId, request.CostEstimateId);
            }

            await itemFileRepo.InsertRange(itemFiles);
            await itemFileRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully uploaded {FileCount} files to item {ItemId} in cost estimate {CostEstimateId}",
                request.Files.Count, request.ItemId, request.CostEstimateId);

            return createdFileIds;
        }

        private CostEstimateItemFile BuildItemFile(UploadItemFilesCommand request, IFormFile file, int order) =>
            new CostEstimateItemFile
            {
                ItemId = request.ItemId,
                CostEstimateId = request.CostEstimateId,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Order = order,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                IsDeleted = false,
                BlobName = string.Empty
            };

        private static string BuildBlobName(UploadItemFilesCommand request, Guid fileId, string extension) =>
            $"{request.TenantId}/{request.ProjectId}/{request.CostEstimateId}/items/{request.ItemId}/{fileId}{extension}";

        private async Task InvalidateCacheAsync(UploadItemFilesCommand request, CancellationToken cancellationToken)
        {
            await ceCacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);
        }
    }
}
