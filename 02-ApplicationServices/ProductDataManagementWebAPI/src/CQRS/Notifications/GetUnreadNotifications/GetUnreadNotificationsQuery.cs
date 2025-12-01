using Business.Interfaces.WebModels.Notifications;
using CQRS; // Corrected namespace for IRequestQuery

namespace CQRS.Notifications.GetUnreadNotifications
{
    public record GetUnreadNotificationsQuery() : IRequestQuery<IEnumerable<NotificationWeb>>;
}