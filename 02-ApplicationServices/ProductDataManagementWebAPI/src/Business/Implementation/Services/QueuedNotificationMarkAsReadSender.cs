using System.Text.Encodings.Web;
using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    // High-level service used by CQRS to enqueue notification mark as read events
    public class QueuedNotificationMarkAsReadSender : INotificationMarkAsReadSender
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IQueueStorageService queueStorageService;
        private readonly ILogger<QueuedNotificationMarkAsReadSender> logger;

        public QueuedNotificationMarkAsReadSender(
            IQueueStorageService queueStorageService, 
            ILogger<QueuedNotificationMarkAsReadSender> logger)
        {
            this.queueStorageService = queueStorageService;
            this.logger = logger;
        }

        public async Task EnqueueAsync(NotificationMarkAsReadDto notificationMarkAsRead, CancellationToken cancellationToken = default)
        {
            await queueStorageService.EnsureQueueAsync(QueueNames.NotificationMarkAsRead, cancellationToken);

            string payload = JsonSerializer.Serialize(notificationMarkAsRead, JsonOptions);
            await queueStorageService.EnqueueAsync(QueueNames.NotificationMarkAsRead, payload, cancellationToken: cancellationToken);
            logger.LogInformation("Notification mark as read enqueued for notification {NotificationId} user {UserId}", 
                notificationMarkAsRead.NotificationId, notificationMarkAsRead.UserId);
        }
    }
}
