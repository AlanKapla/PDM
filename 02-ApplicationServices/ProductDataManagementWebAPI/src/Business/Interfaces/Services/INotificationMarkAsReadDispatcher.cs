using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    public interface INotificationMarkAsReadDispatcher
    {
        Task DispatchAsync(NotificationMarkAsReadDto notificationMarkAsRead, CancellationToken cancellationToken);
    }
}
