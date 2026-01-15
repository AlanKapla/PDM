using MediatR;

namespace CQRS.Notifications.MarkAllNotificationsAsRead
{
    public record MarkAllNotificationsAsReadCommand : IRequestCommand<int>;
}
