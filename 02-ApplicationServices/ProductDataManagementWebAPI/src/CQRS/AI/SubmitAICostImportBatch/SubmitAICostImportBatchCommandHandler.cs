using System.Security.Cryptography;
using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using CQRS.AI.Shared;
using Entities.Enums;
using Entities.Models.AI;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using EntityCostDocumentType = Entities.Enums.CostDocumentType;

namespace CQRS.AI.SubmitAICostImportBatch
{
    public sealed class SubmitAICostImportBatchCommandHandler
        : IRequestHandler<SubmitAICostImportBatchCommand, AICostImportSubmitResultWeb>
    {
        private readonly IRepository<AICostImportBatch> batchRepo;
        private readonly IRepository<AICostImportItem> itemRepo;
        private readonly IAICostImportBlobService blobService;
        private readonly IQueueStorageService queueStorage;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SubmitAICostImportBatchCommandHandler> logger;

        public SubmitAICostImportBatchCommandHandler(
            IRepository<AICostImportBatch> batchRepo,
            IRepository<AICostImportItem> itemRepo,
            IAICostImportBlobService blobService,
            IQueueStorageService queueStorage,
            ICurrentUser currentUser,
            ILogger<SubmitAICostImportBatchCommandHandler> logger)
        {
            this.batchRepo = batchRepo;
            this.itemRepo = itemRepo;
            this.blobService = blobService;
            this.queueStorage = queueStorage;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<AICostImportSubmitResultWeb> Handle(
            SubmitAICostImportBatchCommand request,
            CancellationToken cancellationToken)
        {
            List<AICostImportRejectedFileWeb> rejectedFiles = new List<AICostImportRejectedFileWeb>();
            List<IFormFile> acceptedFiles = new List<IFormFile>();

            foreach (IFormFile file in request.Files)
            {
                FileContentValidator.FileValidationResult validation = FileContentValidator.Validate(file);
                if (!validation.IsSuccess)
                {
                    rejectedFiles.Add(new AICostImportRejectedFileWeb
                    {
                        FileName = file.FileName,
                        Reason = validation.FailureReason!
                    });
                    continue;
                }

                acceptedFiles.Add(file);
            }

            if (acceptedFiles.Count == 0)
            {
                throw new ValidationApiException(
                    "Żaden z przesłanych plików nie ma dozwolonego formatu (JPG, PNG, PDF).");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            EntityCostDocumentType entityCostType = AICostImportMapper.ToEntityCostDocumentType(request.CostDocumentType);

            AICostImportBatch batch = new AICostImportBatch
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                CreatedByUserId = currentUser.Id,
                CostDocumentType = entityCostType,
                TrackedCostContextJson = request.TrackedCostContext is not null
                    ? JsonSerializer.Serialize(request.TrackedCostContext)
                    : null,
                Status = AICostImportBatchStatus.Queued,
                TotalFiles = acceptedFiles.Count,
                CreatedAt = now
            };

            await batchRepo.Insert(batch);
            await batchRepo.SaveChangesAsync(cancellationToken);

            List<AICostImportItem> items = new List<AICostImportItem>();

            foreach (IFormFile file in acceptedFiles)
            {
                AICostImportItem item = new AICostImportItem
                {
                    BatchId = batch.Id,
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    Status = AICostImportItemStatus.Queued,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    FileHashSha256 = await ComputeSha256Async(file, cancellationToken),
                    BlobPath = string.Empty,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await itemRepo.Insert(item);
                await itemRepo.SaveChangesAsync(cancellationToken);

                await using Stream stream = file.OpenReadStream();
                string blobPath = await blobService.UploadPendingAsync(
                    request.TenantId,
                    request.ProjectId,
                    item.Id,
                    stream,
                    file.FileName,
                    file.ContentType,
                    cancellationToken);

                item.BlobPath = blobPath;
                await itemRepo.Update(item);
                items.Add(item);
            }

            await itemRepo.SaveChangesAsync(cancellationToken);

            foreach (AICostImportItem item in items)
            {
                AICostImportQueueMessage message = new AICostImportQueueMessage
                {
                    BatchId = batch.Id,
                    ItemId = item.Id
                };

                string messageText = JsonSerializer.Serialize(message);
                await queueStorage.EnqueueAsync(
                    QueueNames.AICostImportProcess,
                    messageText,
                    cancellationToken: cancellationToken);
            }

            logger.LogInformation(
                "Submitted AI cost import batch {BatchId} with {FileCount} files ({RejectedCount} rejected) for project {ProjectId}",
                batch.Id, batch.TotalFiles, rejectedFiles.Count, request.ProjectId);

            return new AICostImportSubmitResultWeb
            {
                BatchId = batch.Id,
                TotalFiles = batch.TotalFiles,
                Message = "Documents are being analyzed in the background.",
                RejectedFiles = rejectedFiles
            };
        }

        private static async Task<string> ComputeSha256Async(IFormFile file, CancellationToken cancellationToken)
        {
            await using Stream stream = file.OpenReadStream();
            byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
