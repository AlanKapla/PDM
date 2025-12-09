using Business.Interfaces.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    public interface INotificationClient
    {
        Task ReceiveNotification(NotificationDto notification);
        Task ReceiveNotificationMarkAsRead(NotificationMarkAsReadDto notificationMarkAsRead);
    }

    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
    }
}
