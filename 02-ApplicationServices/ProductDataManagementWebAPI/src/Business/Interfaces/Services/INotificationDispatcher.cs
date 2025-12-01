using System.Threading;
using System.Threading.Tasks;
using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    public interface INotificationDispatcher
    {
        Task DispatchAsync(NotificationDto notification, CancellationToken cancellationToken);
    }
}
