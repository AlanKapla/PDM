using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRNotificationMarkAsReadDispatcher : INotificationMarkAsReadDispatcher
    {
        private readonly IHubContext<NotificationHub, INotificationClient> hubContext;
        private readonly ILogger<SignalRNotificationMarkAsReadDispatcher> logger;

        public SignalRNotificationMarkAsReadDispatcher(
            IHubContext<NotificationHub, INotificationClient> hubContext,
            ILogger<SignalRNotificationMarkAsReadDispatcher> logger)
        {
            this.hubContext = hubContext;
            this.logger = logger;
        }

        public async Task DispatchAsync(NotificationMarkAsReadDto notificationMarkAsRead, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(notificationMarkAsRead.AzureAdB2CObjectId))
            {
                logger.LogWarning(
                    "Cannot dispatch notification mark as read for notification {NotificationId} - AzureAdB2CObjectId is missing for user {UserId}",
                    notificationMarkAsRead.NotificationId,
                    notificationMarkAsRead.UserId);
                return;
            }

            await hubContext.Clients.User(notificationMarkAsRead.AzureAdB2CObjectId)
                .ReceiveNotificationMarkAsRead(notificationMarkAsRead);

            logger.LogInformation(
                "Notification mark as read for {NotificationId} dispatched to user {AzureAdB2CObjectId}",
                notificationMarkAsRead.NotificationId,
                notificationMarkAsRead.AzureAdB2CObjectId);
        }
    }
}
