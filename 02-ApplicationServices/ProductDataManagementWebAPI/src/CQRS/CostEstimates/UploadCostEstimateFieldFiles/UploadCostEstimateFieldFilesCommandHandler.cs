using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UploadCostEstimateFieldFiles
{
    public class UploadCostEstimateFieldFilesCommandHandler : IRequestHandler<UploadCostEstimateFieldFilesCommand, List<Guid>>
    {
        private readonly IReadRepository<CostEstimateItemFieldValue> fieldValueReadRepo;
        private readonly IRepository<CostEstimateItemFieldValue> fieldValueRepo;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICacheService cacheService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadCostEstimateFieldFilesCommandHandler> logger;

        public UploadCostEstimateFieldFilesCommandHandler(
            IReadRepository<CostEstimateItemFieldValue> fieldValueReadRepo,
            IRepository<CostEstimateItemFieldValue> fieldValueRepo,
            IRepository<CostEstimateFieldFile> fieldFileRepo,
            IBlobStorageService blobStorageService,
            ICostEstimateCacheService ceCacheService,
            ICacheService cacheService,
            ICurrentUser currentUser,
            ILogger<UploadCostEstimateFieldFilesCommandHandler> logger)
        {
            this.fieldValueReadRepo = fieldValueReadRepo;
            this.fieldValueRepo = fieldValueRepo;
            this.fieldFileRepo = fieldFileRepo;
            this.blobStorageService = blobStorageService;
            this.ceCacheService = ceCacheService;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<List<Guid>> Handle(UploadCostEstimateFieldFilesCommand request, CancellationToken cancellationToken)
        {
            // Validate cost estimate via cache (includes owner check)
            var costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // Validate item belongs to cost estimate via cached items
            var itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }

            // Validate field definition via cached template
            var template = await ceCacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            var fieldDef = template.SystemFieldDefinitions
                .FirstOrDefault(fd => fd.Id == request.FieldDefinitionId &&
                                      fd.FieldType == FieldType.ItemSystemFiles)
                ?? throw new ValidationApiException("Field definition not found or is not of type ItemSystemFiles");

            // Find or create field value for this item + field definition
            var fieldValue = await fieldValueReadRepo.GetFirstBySearch(
                fv => fv.ItemId == request.ItemId &&
                      fv.FieldDefinitionId == request.FieldDefinitionId,
                cancellationToken);

            if (fieldValue == null)
            {
                fieldValue = new CostEstimateItemFieldValue
                {
                    ItemId = request.ItemId,
                    FieldDefinitionId = request.FieldDefinitionId,
                    CreatedAt = DateTime.UtcNow
                };

                await fieldValueRepo.Insert(fieldValue);
                await fieldValueRepo.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Created new field value (ID: {FieldValueId}) for item {ItemId} with field definition {FieldDefinitionId}",
                    fieldValue.Id, request.ItemId, request.FieldDefinitionId);
            }

            // --- REPLACE ALL: delete existing files (DB soft-delete + blob delete) ---
            await DeleteExistingFilesAsync(fieldValue.Id, request.CostEstimateId, cancellationToken);

            // --- Upload new files ---
            var createdFileIds = new List<Guid>();
            if (request.Files.Count > 0)
            {
                createdFileIds = await UploadNewFilesAsync(request, fieldValue.Id, cancellationToken);
            }

            // Invalidate SAS URI cache and item field values cache
            string sasCacheKey = $"ce-files-sas:{request.CostEstimateId}";
            await cacheService.RemoveCacheByKeyAsync(sasCacheKey, cancellationToken);
            await ceCacheService.InvalidateItemFieldValuesAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return createdFileIds;
        }

        private async Task DeleteExistingFilesAsync(Guid fieldValueId, Guid costEstimateId, CancellationToken cancellationToken)
        {
            var existingFiles = await fieldFileRepo.GetBySearch(
                f => f.FieldValueId == fieldValueId &&
                     f.CostEstimateId == costEstimateId &&
                     !f.IsDeleted);

            var fileList = existingFiles.ToList();
            if (fileList.Count == 0)
            {
                return;
            }

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);
            var now = DateTime.UtcNow;

            foreach (var file in fileList)
            {
                file.IsDeleted = true;
                file.DeletedAt = now;
            }

            await fieldFileRepo.UpdateRange(fileList);

            // Delete from Blob Storage
            foreach (var file in fileList)
            {
                await blobStorageService.DeleteAsync(containerName, file.BlobName, cancellationToken);
            }

            await fieldFileRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Replaced {FileCount} existing files (soft-deleted + blob removed) for field value {FieldValueId} in cost estimate {CostEstimateId}",
                fileList.Count, fieldValueId, costEstimateId);
        }

        private async Task<List<Guid>> UploadNewFilesAsync(
            UploadCostEstimateFieldFilesCommand request,
            Guid fieldValueId,
            CancellationToken cancellationToken)
        {
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);
            var createdFileIds = new List<Guid>();

            var fieldFiles = new List<CostEstimateFieldFile>();

            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                string fileExtension = Path.GetExtension(file.FileName);

                var fieldFile = new CostEstimateFieldFile
                {
                    FieldValueId = fieldValueId,
                    CostEstimateId = request.CostEstimateId,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    Order = i,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = currentUser.Id,
                    IsDeleted = false
                };

                string blobName = $"{request.TenantId}/{request.ProjectId}/{request.CostEstimateId}/{fieldValueId}/{fieldFile.Id}{fileExtension}";
                fieldFile.BlobName = blobName;

                using (var stream = file.OpenReadStream())
                {
                    await blobStorageService.UploadAsync(
                        containerName,
                        blobName,
                        stream,
                        file.ContentType,
                        cancellationToken);
                }

                fieldFiles.Add(fieldFile);
                createdFileIds.Add(fieldFile.Id);

                logger.LogInformation(
                    "Uploaded cost estimate field file {FileName} (ID: {FileId}) for item {ItemId} in cost estimate {CostEstimateId}",
                    file.FileName, fieldFile.Id, request.ItemId, request.CostEstimateId);
            }

            await fieldFileRepo.InsertRange(fieldFiles);
            await fieldFileRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully uploaded {FileCount} files to item {ItemId} (field value {FieldValueId}) in cost estimate {CostEstimateId}",
                request.Files.Count, request.ItemId, fieldValueId, request.CostEstimateId);

            return createdFileIds;
        }
    }
}
