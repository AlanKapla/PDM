using Business.Interfaces.WebModels.Notifications;

namespace CQRS.Notifications.GetAllNotifications
{
    public sealed record GetAllNotificationsQuery : IRequestQuery<IEnumerable<NotificationWeb>>
    {
        public int Take { get; init; } = 50;
        public int Skip { get; init; } = 0;
    }
}
