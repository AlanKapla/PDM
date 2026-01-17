using Business.Interfaces.WebModels.Notifications;

namespace CQRS.Notifications.GetAllNotifications
{
    public record GetAllNotificationsQuery(int Take = 50, int Skip = 0) : IRequestQuery<IEnumerable<NotificationWeb>>;
}
