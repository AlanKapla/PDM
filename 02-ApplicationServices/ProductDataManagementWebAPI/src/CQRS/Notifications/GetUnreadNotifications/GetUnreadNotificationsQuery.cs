using Business.Interfaces.WebModels.Notifications;
using CQRS;

namespace CQRS.Notifications.GetUnreadNotifications
{
    public record GetUnreadNotificationsQuery(int Take = 50, int Skip = 0) : IRequestQuery<IEnumerable<NotificationWeb>>;
}
