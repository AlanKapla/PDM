using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.AI;
using Entities.Models.Notifications;
using Entities.Models.Users;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotifType = Business.Interfaces.DTO.NotificationType;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostImportNotificationService : IAICostImportNotificationService
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ILogger<AICostImportNotificationService> logger;

        public AICostImportNotificationService(
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ILogger<AICostImportNotificationService> logger)
        {
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task NotifyBatchCompletedAsync(
            AICostImportBatch batch,
            CancellationToken cancellationToken)
        {
            try
            {
                User? user = await userRepo.GetFirstBySearch(
                    u => u.Id == batch.CreatedByUserId,
                    cancellationToken);

                NotifType type = batch.ErrorCount > 0 ? NotifType.Warning : NotifType.Info;
                string title = "Analiza dokumentów kosztowych zakończona";
                string message =
                    $"Przeanalizowano {batch.TotalFiles} dokumentów. " +
                    $"{batch.PendingCount} oczekuje na akceptację, " +
                    $"{batch.ErrorCount} wymaga ręcznej weryfikacji, " +
                    $"{batch.DuplicateCount} wykryto jako możliwe duplikaty.";

                string reviewRoute = batch.CostDocumentType == CostDocumentType.ProjectCost
                    ? $"/projects/{batch.ProjectId}/costs/ai-review"
                    : $"/projects/{batch.ProjectId}/dashboard/ai-review";

                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = batch.TenantId,
                    ProjectId = batch.ProjectId,
                    UserId = batch.CreatedByUserId,
                    AzureAdB2CObjectId = user?.AzureAdB2CObjectId,
                    Type = type,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["route"] = reviewRoute,
                        ["batchId"] = batch.Id,
                        ["pendingCount"] = batch.PendingCount,
                        ["errorCount"] = batch.ErrorCount,
                        ["duplicateCount"] = batch.DuplicateCount
                    }
                };

                if (user is null)
                {
                    logger.LogWarning(
                        "AI cost import notification fallback: user lookup failed for {UserId}. Persisting DB notification without SignalR target.",
                        batch.CreatedByUserId);
                }

                int unreadCount = await notificationRepo.CountAsync(
                    n => n.UserId == notification.UserId && !n.IsRead,
                    cancellationToken) + 1;

                NotificationPayloadDto payload = new NotificationPayloadDto(notification, unreadCount);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send AI cost import batch completion notification for batch {BatchId}",
                    batch.Id);
            }
        }
    }
}
