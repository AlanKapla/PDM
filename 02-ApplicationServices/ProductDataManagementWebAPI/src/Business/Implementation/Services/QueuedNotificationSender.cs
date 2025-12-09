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

        public async Task EnqueueAsync(NotificationDto notificationDto, CancellationToken cancellationToken = default)
        {
            // Load Tenant and Project to get names
            Tenant? tenant = await tenantRepo.GetFirstBySearch(t => t.Id == notificationDto.TenantId);
            if (tenant != null)
            {
                notificationDto.TenantName = tenant.Name;
            }

            if (notificationDto.ProjectId.HasValue)
            {
                Project? project = await projectRepo.GetFirstBySearch(p => p.Id == notificationDto.ProjectId.Value);
                if (project != null)
                {
                    notificationDto.ProjectName = project.Name;
                }
            }

            // 1) Persist in DB for history and UI read
            Notification entity = new Notification
            {
                Id = notificationDto.Id,
                TenantId = notificationDto.TenantId,
                ProjectId = notificationDto.ProjectId,
                UserId = notificationDto.UserId,
                Type = MapType(notificationDto.Type),
                Title = notificationDto.Title,
                Message = notificationDto.Message,
                CreatedAt = notificationDto.CreatedAt == default ? DateTimeOffset.UtcNow : notificationDto.CreatedAt,
                Readed = notificationDto.Readed,
                MetadataJson = notificationDto.Metadata != null ? JsonSerializer.Serialize(notificationDto.Metadata) : null
            };

            await notificationRepo.Insert(entity).ConfigureAwait(false);

            await queueStorageService.EnsureQueueAsync(QueueNames.NotificationSend, cancellationToken);

            string payload = JsonSerializer.Serialize(notificationDto, JsonOptions);
            await queueStorageService.EnqueueAsync(QueueNames.NotificationSend, payload, cancellationToken: cancellationToken);
            logger.LogInformation("Notification enqueued for user {UserId} type {Type}", notificationDto.UserId, notificationDto.Type);
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
