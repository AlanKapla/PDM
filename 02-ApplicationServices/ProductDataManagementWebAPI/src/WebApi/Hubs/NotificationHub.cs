using Business.Interfaces.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    public interface INotificationClient
    {
        Task ReceiveNotification(NotificationPayloadDto payload);
        Task ReceiveNotificationMarkAsRead(NotificationMarkAsReadDto notificationMarkAsRead);
    }

    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // Metoda diagnostyczna - pokazuje czy SignalR widzi UserIdentifier
        public string WhoAmI()
        {
            return Context.UserIdentifier ?? "NULL";
        }

        // Health check - frontend może pingować, żeby utrzymać połączenie
        public Task Ping()
        {
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "SignalR connected: User={UserIdentifier}, ConnectionId={ConnectionId}",
                Context.UserIdentifier ?? "ANONYMOUS",
                Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "SignalR disconnected with error: User={UserIdentifier}, ConnectionId={ConnectionId}",
                    Context.UserIdentifier ?? "ANONYMOUS",
                    Context.ConnectionId);
            }
            else
            {
                _logger.LogDebug(
                    "SignalR disconnected: User={UserIdentifier}, ConnectionId={ConnectionId}",
                    Context.UserIdentifier ?? "ANONYMOUS",
                    Context.ConnectionId);
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
