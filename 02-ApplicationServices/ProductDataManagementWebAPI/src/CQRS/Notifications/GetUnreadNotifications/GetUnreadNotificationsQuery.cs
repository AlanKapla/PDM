using Business.Interfaces.WebModels.Notifications;
using CQRS;

namespace CQRS.Notifications.GetUnreadNotifications
{
    public sealed record GetUnreadNotificationsQuery : IRequestQuery<IEnumerable<NotificationWeb>>
    {
        public int Take { get; init; } = 50;
        public int Skip { get; init; } = 0;
    }
}
