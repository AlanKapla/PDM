using Business.Interfaces.WebModels.Notifications;
using CQRS;

namespace CQRS.Notifications.GetAllNotifications
{
    public record GetAllNotificationsQuery(int Limit = 50) : IRequestQuery<IEnumerable<NotificationWeb>>;
}
