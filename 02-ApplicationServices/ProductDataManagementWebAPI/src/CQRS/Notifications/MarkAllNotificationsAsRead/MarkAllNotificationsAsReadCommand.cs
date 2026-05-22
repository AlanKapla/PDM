using MediatR;

namespace CQRS.Notifications.MarkAllNotificationsAsRead
{
    public sealed record MarkAllNotificationsAsReadCommand : IRequestCommand<int>;
}
