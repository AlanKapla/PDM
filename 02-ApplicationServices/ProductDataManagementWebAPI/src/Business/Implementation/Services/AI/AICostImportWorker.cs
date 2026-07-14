using System.Text.Json;
using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostImportWorker : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IQueueStorageService queueStorage;
        private readonly IOptions<AICostImportOptions> options;
        private readonly ILogger<AICostImportWorker> logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AICostImportWorker(
            IServiceProvider serviceProvider,
            IQueueStorageService queueStorage,
            IOptions<AICostImportOptions> options,
            ILogger<AICostImportWorker> logger)
        {
            this.serviceProvider = serviceProvider;
            this.queueStorage = queueStorage;
            this.options = options;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string queueName = options.Value.QueueName;
            await queueStorage.EnsureQueueAsync(queueName, stoppingToken);
            logger.LogInformation("AICostImportWorker started. Listening on queue {QueueName}", queueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DequeuedMessage? message = await queueStorage.DequeueAsync(
                        queueName, cancellationToken: stoppingToken);

                    if (message is null)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(options.Value.WorkerPollIntervalSeconds),
                            stoppingToken);
                        continue;
                    }

                    await ProcessMessageAsync(message, queueName, stoppingToken);
                    await queueStorage.DeleteMessageAsync(
                        queueName, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing AI cost import queue message");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }

            logger.LogInformation("AICostImportWorker stopped.");
        }

        private async Task ProcessMessageAsync(
            DequeuedMessage message,
            string queueName,
            CancellationToken stoppingToken)
        {
            AICostImportQueueMessage? payload = JsonSerializer.Deserialize<AICostImportQueueMessage>(
                message.Text, JsonOptions);

            if (payload is null)
            {
                logger.LogWarning("Invalid AI cost import queue message: {Text}", message.Text);
                return;
            }

            using IServiceScope scope = serviceProvider.CreateScope();
            IRepository<AICostImportItem> itemRepo =
                scope.ServiceProvider.GetRequiredService<IRepository<AICostImportItem>>();
            IRepository<AICostImportBatch> batchRepo =
                scope.ServiceProvider.GetRequiredService<IRepository<AICostImportBatch>>();
            IDocumentParserService parserService =
                scope.ServiceProvider.GetRequiredService<IDocumentParserService>();
            IAICostDocumentEnrichmentService enrichmentService =
                scope.ServiceProvider.GetRequiredService<IAICostDocumentEnrichmentService>();
            IAICostDuplicateDetectionService duplicateService =
                scope.ServiceProvider.GetRequiredService<IAICostDuplicateDetectionService>();
            IAICostImportBlobService blobService =
                scope.ServiceProvider.GetRequiredService<IAICostImportBlobService>();
            IAICostImportNotificationService notificationService =
                scope.ServiceProvider.GetRequiredService<IAICostImportNotificationService>();

            AICostImportItem? item = await itemRepo.GetFirstBySearch(
                i => i.Id == payload.ItemId && i.BatchId == payload.BatchId);

            if (item is null)
            {
                logger.LogWarning(
                    "AI cost import item {ItemId} in batch {BatchId} not found",
                    payload.ItemId, payload.BatchId);
                return;
            }

            if (item.Status is AICostImportItemStatus.Pending
                or AICostImportItemStatus.Accepted
                or AICostImportItemStatus.ErrorNeedsReview
                or AICostImportItemStatus.DuplicateDetected)
            {
                return;
            }

            AICostImportBatch? batch = await batchRepo.GetFirstBySearch(b => b.Id == item.BatchId);
            if (batch is null)
            {
                return;
            }

            if (batch.Status == AICostImportBatchStatus.Queued)
            {
                batch.Status = AICostImportBatchStatus.Processing;
                await batchRepo.Update(batch);
                await batchRepo.SaveChangesAsync(stoppingToken);
            }

            item.Status = AICostImportItemStatus.Processing;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);
            await itemRepo.SaveChangesAsync(stoppingToken);

            try
            {
                await ProcessItemAsync(
                    item, batch, parserService, enrichmentService,
                    duplicateService, blobService, itemRepo, batchRepo, stoppingToken);

                await TryCompleteBatchAsync(batch, batchRepo, notificationService, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to process AI cost import item {ItemId}, retry {RetryCount}",
                    item.Id, item.RetryCount);

                await HandleFailureAsync(
                    item, batch, ex.Message, itemRepo, batchRepo,
                    queueStorage, queueName, notificationService, stoppingToken);
            }
        }

        private async Task ProcessItemAsync(
            AICostImportItem item,
            AICostImportBatch batch,
            IDocumentParserService parserService,
            IAICostDocumentEnrichmentService enrichmentService,
            IAICostDuplicateDetectionService duplicateService,
            IAICostImportBlobService blobService,
            IRepository<AICostImportItem> itemRepo,
            IRepository<AICostImportBatch> batchRepo,
            CancellationToken cancellationToken)
        {
            BlobDownload download = await blobService.DownloadPendingAsync(item.BlobPath, cancellationToken);
            byte[] fileBytes;
            await using (download.Content)
            {
                using MemoryStream ms = new MemoryStream();
                await download.Content.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }

            ParsedCostDto parsed = await parserService.ParseAsync(
                fileBytes, item.ContentType, cancellationToken);

            if (parsed.Confidence == 0)
            {
                throw new InvalidOperationException("Document parser returned zero confidence.");
            }

            parsed = await enrichmentService.EnrichWithContractorAsync(
                parsed, item.TenantId, cancellationToken);
            parsed = await enrichmentService.EnrichWithCategoryAsync(
                parsed, item.ProjectId, cancellationToken);

            bool isDuplicate = await duplicateService.IsDuplicateAsync(
                item.TenantId, item.ProjectId, item.FileHashSha256,
                parsed, item.Id, cancellationToken);

            if (isDuplicate)
            {
                await HandleDuplicateAsync(
                    item, batch, parsed, itemRepo, batchRepo, cancellationToken);
                return;
            }

            item.ParsedDataJson = JsonSerializer.Serialize(parsed);
            item.Status = AICostImportItemStatus.Pending;
            item.AnalyzedAt = DateTimeOffset.UtcNow;
            item.LastError = null;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);

            batch.ProcessedFiles++;
            batch.PendingCount++;
            await batchRepo.Update(batch);
            await itemRepo.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleDuplicateAsync(
            AICostImportItem item,
            AICostImportBatch batch,
            ParsedCostDto parsed,
            IRepository<AICostImportItem> itemRepo,
            IRepository<AICostImportBatch> batchRepo,
            CancellationToken cancellationToken)
        {
            item.ParsedDataJson = JsonSerializer.Serialize(parsed);
            item.Status = AICostImportItemStatus.DuplicateDetected;
            item.AnalyzedAt = DateTimeOffset.UtcNow;
            item.LastError = null;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);

            batch.ProcessedFiles++;
            batch.DuplicateCount++;
            await batchRepo.Update(batch);
            await itemRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Marked AI cost import item {ItemId} in batch {BatchId} as duplicate for user review",
                item.Id, batch.Id);
        }

        private async Task HandleFailureAsync(
            AICostImportItem item,
            AICostImportBatch batch,
            string errorMessage,
            IRepository<AICostImportItem> itemRepo,
            IRepository<AICostImportBatch> batchRepo,
            IQueueStorageService queue,
            string queueName,
            IAICostImportNotificationService notificationService,
            CancellationToken cancellationToken)
        {
            item.RetryCount++;
            item.LastError = errorMessage;
            item.UpdatedAt = DateTimeOffset.UtcNow;

            if (item.RetryCount < options.Value.MaxRetryAttempts)
            {
                item.Status = AICostImportItemStatus.Queued;
                await itemRepo.Update(item);
                await itemRepo.SaveChangesAsync(cancellationToken);

                double delaySeconds = options.Value.InitialRetryDelaySeconds
                    * Math.Pow(options.Value.RetryBackoffMultiplier, item.RetryCount - 1);

                AICostImportQueueMessage retryMessage = new AICostImportQueueMessage
                {
                    BatchId = item.BatchId,
                    ItemId = item.Id
                };

                string messageText = JsonSerializer.Serialize(retryMessage);
                await queue.EnqueueAsync(
                    queueName,
                    messageText,
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Scheduled retry {RetryCount} for AI cost import item {ItemId} in {DelaySeconds}s",
                    item.RetryCount, item.Id, delaySeconds);
                return;
            }

            item.Status = AICostImportItemStatus.ErrorNeedsReview;
            item.AnalyzedAt = DateTimeOffset.UtcNow;
            await itemRepo.Update(item);

            batch.ProcessedFiles++;
            batch.ErrorCount++;
            await batchRepo.Update(batch);
            await itemRepo.SaveChangesAsync(cancellationToken);

            await TryCompleteBatchAsync(batch, batchRepo, notificationService, cancellationToken);
        }

        private async Task TryCompleteBatchAsync(
            AICostImportBatch batch,
            IRepository<AICostImportBatch> batchRepo,
            IAICostImportNotificationService notificationService,
            CancellationToken cancellationToken)
        {
            if (batch.ProcessedFiles < batch.TotalFiles)
            {
                return;
            }

            batch.Status = AICostImportBatchStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            await batchRepo.Update(batch);
            await batchRepo.SaveChangesAsync(cancellationToken);

            await notificationService.NotifyBatchCompletedAsync(batch, cancellationToken);

            logger.LogInformation(
                "AI cost import batch {BatchId} completed. Pending={Pending}, Errors={Errors}, Duplicates={Duplicates}",
                batch.Id, batch.PendingCount, batch.ErrorCount, batch.DuplicateCount);
        }
    }
}
