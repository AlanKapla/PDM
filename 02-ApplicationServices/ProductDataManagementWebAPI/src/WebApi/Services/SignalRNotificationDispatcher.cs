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

        public async Task DispatchAsync(NotificationDto notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(notification.AzureAdB2CObjectId))
            {
                logger.LogWarning(
                    "Cannot dispatch notification {NotificationId} - AzureAdB2CObjectId is missing for user {UserId}",
                    notification.Id,
                    notification.UserId);
                return;
            }

            await hubContext.Clients.User(notification.AzureAdB2CObjectId)
                .ReceiveNotification(notification);

            logger.LogInformation(
                "Notification {NotificationId} dispatched to user {AzureAdB2CObjectId}",
                notification.Id,
                notification.AzureAdB2CObjectId);
        }
    }
}
