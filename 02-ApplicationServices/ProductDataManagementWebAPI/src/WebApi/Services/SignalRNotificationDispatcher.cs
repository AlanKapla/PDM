using System.Threading;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRNotificationDispatcher : INotificationDispatcher
    {
        private readonly IHubContext<NotificationHub, INotificationClient> hubContext;

        public SignalRNotificationDispatcher(IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task DispatchAsync(NotificationDto notification, CancellationToken cancellationToken)
        {
            string userId = notification.UserId.ToString();
            await hubContext.Clients.User(userId)
                .ReceiveNotification(notification);
        }
    }
}
