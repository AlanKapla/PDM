using System.Threading;
using System.Threading.Tasks;
using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    // API for CQRS handlers to publish notifications to the queue
    public interface INotificationSender
    {
        Task EnqueueAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    }
}
