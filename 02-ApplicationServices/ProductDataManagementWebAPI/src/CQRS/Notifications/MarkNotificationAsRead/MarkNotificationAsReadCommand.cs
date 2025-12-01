using MediatR;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequestCommand<Unit>;
}
