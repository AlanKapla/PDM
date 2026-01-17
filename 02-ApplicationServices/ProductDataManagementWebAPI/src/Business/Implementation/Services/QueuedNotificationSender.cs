using System.Text.Json;
using System.Text.Encodings.Web;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Entities.Models;
using Repositiories.Repository.Interfaces;

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
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<Project> projectRepo;

        public QueuedNotificationSender(
            IQueueStorageService queueStorageService, 
            ILogger<QueuedNotificationSender> logger, 
            IReadRepository<Notification> notificationRepo,
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
                CreatedAt = notification.CreatedAt == default ? DateTimeOffset.UtcNow : notification.CreatedAt,
                Readed = notification.Readed,
                MetadataJson = notification.Metadata != null ? JsonSerializer.Serialize(notification.Metadata) : null
            };

            await notificationRepo.Insert(entity);

            await queueStorageService.EnsureQueueAsync(QueueNames.NotificationSend, cancellationToken);

            string queuePayload = JsonSerializer.Serialize(payload, JsonOptions);
            await queueStorageService.EnqueueAsync(QueueNames.NotificationSend, queuePayload, cancellationToken: cancellationToken);
            
            var elapsedMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            logger.LogInformation("✅ [ENQUEUE DONE] NotificationId={NotificationId}, UnreadCounter={UnreadCounter}, Elapsed={ElapsedMs}ms, Timestamp={Timestamp}",
                notification.Id, payload.UnreadNotificationCounter, elapsedMs, DateTimeOffset.UtcNow);
        }

        private static Entities.Models.NotificationType MapType(Business.Interfaces.DTO.NotificationType type)
        {
            return type switch
            {
                Interfaces.DTO.NotificationType.Info => Entities.Models.NotificationType.Info,
                Interfaces.DTO.NotificationType.Success => Entities.Models.NotificationType.Success,
                Interfaces.DTO.NotificationType.Warning => Entities.Models.NotificationType.Warning,
                Interfaces.DTO.NotificationType.Error => Entities.Models.NotificationType.Error,
                _ => Entities.Models.NotificationType.Info
            };
        }
    }
}
