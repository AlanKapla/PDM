using System.Threading;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRNotificationDispatcher : INotificationDispatcher
    {
        private readonly IHubContext<NotificationHub, INotificationClient> hubContext;
        private readonly ILogger<SignalRNotificationDispatcher> logger;

        public SignalRNotificationDispatcher(
            IHubContext<NotificationHub, INotificationClient> hubContext,
            ILogger<SignalRNotificationDispatcher> logger)
        {
            this.hubContext = hubContext;
            this.logger = logger;
        }

        public async Task DispatchAsync(NotificationPayloadDto payload, CancellationToken cancellationToken)
        {
            var notification = payload.Notification;
            
            logger.LogInformation("📨 [SignalR] Attempting to dispatch notification {NotificationId} to user {AzureAdB2CObjectId} with unread count {UnreadCount}",
                notification.Id,
                notification.AzureAdB2CObjectId,
                payload.UnreadNotificationCounter);

            if (string.IsNullOrEmpty(notification.AzureAdB2CObjectId))
            {
                logger.LogWarning(
                    "❌ [SignalR] Cannot dispatch notification {NotificationId} - AzureAdB2CObjectId is missing for user {UserId}",
                    notification.Id,
                    notification.UserId);
                return;
            }

            try
            {
                await hubContext.Clients.User(notification.AzureAdB2CObjectId)
                    .ReceiveNotification(payload);

                logger.LogInformation(
                    "✅ [SignalR] Notification {NotificationId} dispatched to user {AzureAdB2CObjectId} with unread count {UnreadCount}",
                    notification.Id,
                    notification.AzureAdB2CObjectId,
                    payload.UnreadNotificationCounter);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "❌ [SignalR] Failed to dispatch notification {NotificationId} to user {AzureAdB2CObjectId}",
                    notification.Id,
                    notification.AzureAdB2CObjectId);
                throw;
            }
        }
    }
}
