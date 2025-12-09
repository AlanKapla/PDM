using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    // API for CQRS handlers to publish notification mark as read events to the queue
    public interface INotificationMarkAsReadSender
    {
        Task EnqueueAsync(NotificationMarkAsReadDto notificationMarkAsRead, CancellationToken cancellationToken = default);
    }
}
