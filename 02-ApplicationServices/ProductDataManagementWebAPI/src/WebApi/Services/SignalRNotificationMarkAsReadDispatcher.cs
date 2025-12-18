using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRNotificationMarkAsReadDispatcher : INotificationMarkAsReadDispatcher
    {
        private readonly IHubContext<NotificationHub, INotificationClient> hubContext;

        public SignalRNotificationMarkAsReadDispatcher(IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task DispatchAsync(NotificationMarkAsReadDto notificationMarkAsRead, CancellationToken cancellationToken)
        {
            string userId = notificationMarkAsRead.UserId.ToString();
            await hubContext.Clients.User(userId)
                .ReceiveNotificationMarkAsRead(notificationMarkAsRead);
        }
    }
}
