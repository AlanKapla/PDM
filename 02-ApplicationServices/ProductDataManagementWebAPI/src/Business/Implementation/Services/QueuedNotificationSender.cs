using Business.Implementation.Utilities;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Business.Implementation.Services
{
    // High-level service used by CQRS to enqueue notifications
    public class QueuedNotificationSender : INotificationSender
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Pozwala na polskie znaki bez escape'owania
        };

        private readonly IQueueStorageService queueStorageService;
        private readonly ILogger<QueuedNotificationSender> logger;
        private readonly IRepository<Notification> notificationRepo;
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<Project> projectRepo;

        public QueuedNotificationSender(
            IQueueStorageService queueStorageService, 
            ILogger<QueuedNotificationSender> logger, 
            IRepository<Notification> notificationRepo,
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<Project> projectRepo)
        {
            this.queueStorageService = queueStorageService;
            this.logger = logger;
            this.notificationRepo = notificationRepo;
            this.tenantRepo = tenantRepo;
            this.projectRepo = projectRepo;
        }

        public async Task EnqueueAsync(NotificationPayloadDto payload, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var notification = payload.Notification;
            
            logger.LogInformation("📤 [ENQUEUE START] NotificationId={NotificationId}, UserId={UserId}, Type={Type}, UnreadCounter={UnreadCounter}, Timestamp={Timestamp}",
                notification.Id, notification.UserId, notification.Type, payload.UnreadNotificationCounter, startTime);
            
            // Load Tenant and Project to get names
            Tenant? tenant = await tenantRepo.GetFirstBySearch(t => t.Id == notification.TenantId, cancellationToken);
            if (tenant != null)
            {
                notification.TenantName = tenant.Name;
            }

            if (notification.ProjectId.HasValue)
            {
                Project? project = await projectRepo.GetFirstBySearch(p => p.Id == notification.ProjectId.Value, cancellationToken);
                if (project != null)
                {
                    notification.ProjectName = project.Name;
                }
            }

            // Persist in DB for history and UI read
            Notification entity = new Notification
            {
                Id = notification.Id,
                TenantId = notification.TenantId,
                ProjectId = notification.ProjectId,
                UserId = notification.UserId,
                Type = MapType(notification.Type),
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = UtcDateTimeHelper.SpecifyUtc(
                    notification.CreatedAt == default ? DateTime.UtcNow : notification.CreatedAt),
                IsRead = notification.IsRead,
                MetadataJson = notification.Metadata != null ? JsonSerializer.Serialize(notification.Metadata) : null
            };

            await notificationRepo.Insert(entity);
            await notificationRepo.SaveChangesAsync(cancellationToken);

            await queueStorageService.EnsureQueueAsync(QueueNames.NotificationSend, cancellationToken);

            string queuePayload = JsonSerializer.Serialize(payload, JsonOptions);
            await queueStorageService.EnqueueAsync(QueueNames.NotificationSend, queuePayload, cancellationToken: cancellationToken);
            
            var elapsedMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            logger.LogInformation("✅ [ENQUEUE DONE] NotificationId={NotificationId}, UnreadCounter={UnreadCounter}, Elapsed={ElapsedMs}ms, Timestamp={Timestamp}",
                notification.Id, payload.UnreadNotificationCounter, elapsedMs, DateTimeOffset.UtcNow);
        }

        private static Entities.Models.Notifications.NotificationType MapType(Business.Interfaces.DTO.NotificationType type)
        {
            return type switch
            {
                Interfaces.DTO.NotificationType.Info => Entities.Models.Notifications.NotificationType.Info,
                Interfaces.DTO.NotificationType.Success => Entities.Models.Notifications.NotificationType.Success,
                Interfaces.DTO.NotificationType.Warning => Entities.Models.Notifications.NotificationType.Warning,
                Interfaces.DTO.NotificationType.Error => Entities.Models.Notifications.NotificationType.Error,
                _ => Entities.Models.Notifications.NotificationType.Info
            };
        }
    }
}
