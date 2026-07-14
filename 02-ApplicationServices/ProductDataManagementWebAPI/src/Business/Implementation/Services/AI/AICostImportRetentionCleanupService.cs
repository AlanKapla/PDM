using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostImportRetentionCleanupService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<AICostImportOptions> options;
        private readonly ILogger<AICostImportRetentionCleanupService> logger;

        public AICostImportRetentionCleanupService(
            IServiceProvider serviceProvider,
            IOptions<AICostImportOptions> options,
            ILogger<AICostImportRetentionCleanupService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("AICostImportRetentionCleanupService started. Will run daily at 2:00 AM UTC.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime nextRun = now.Date.AddDays(1).AddHours(2);

                    if (now.Hour < 2)
                    {
                        nextRun = now.Date.AddHours(2);
                    }

                    TimeSpan delay = nextRun - now;
                    logger.LogInformation(
                        "Next AI cost import retention cleanup scheduled for {NextRun} UTC",
                        nextRun);

                    await Task.Delay(delay, stoppingToken);
                    await CleanupExpiredItemsAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in AICostImportRetentionCleanupService");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            logger.LogInformation("AICostImportRetentionCleanupService stopped.");
        }

        private async Task CleanupExpiredItemsAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting AI cost import retention cleanup...");

            using IServiceScope scope = serviceProvider.CreateScope();
            IRepository<AICostImportItem> itemRepo =
                scope.ServiceProvider.GetRequiredService<IRepository<AICostImportItem>>();
            IAICostImportBlobService blobService =
                scope.ServiceProvider.GetRequiredService<IAICostImportBlobService>();

            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.RetentionDays);

            IEnumerable<AICostImportItem> expiredItems = await itemRepo.GetBySearch(
                i => (i.Status == AICostImportItemStatus.Pending
                      || i.Status == AICostImportItemStatus.ErrorNeedsReview
                      || i.Status == AICostImportItemStatus.DuplicateDetected)
                     && i.AnalyzedAt != null
                     && i.AnalyzedAt < cutoff);

            List<AICostImportItem> items = expiredItems.ToList();
            if (items.Count == 0)
            {
                logger.LogInformation("AI cost import retention cleanup: no expired items found");
                return;
            }

            int deletedCount = 0;
            foreach (AICostImportItem item in items)
            {
                try
                {
                    await blobService.DeletePendingAsync(item.BlobPath, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete blob for expired AI cost import item {ItemId}",
                        item.Id);
                }

                await itemRepo.Delete(item);
                deletedCount++;

                logger.LogInformation(
                    "Expired AI cost import item deleted: ItemId={ItemId}, BatchId={BatchId}, TenantId={TenantId}, ProjectId={ProjectId}, FileName={FileName}",
                    item.Id, item.BatchId, item.TenantId, item.ProjectId, item.OriginalFileName);
            }

            await itemRepo.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "AI cost import retention cleanup completed: {DeletedCount} items removed",
                deletedCount);
        }
    }
}
