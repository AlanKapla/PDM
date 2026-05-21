using MediatR;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public sealed record MarkNotificationAsReadCommand : IRequestCommand<Unit>
    {
        public required Guid NotificationId { get; init; }
    }
}
