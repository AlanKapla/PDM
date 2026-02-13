using Business.Interfaces.DTO;
using Entities.Models;
using Repositories.Repository.Interfaces;

namespace CQRS.Helpers
{
    /// <summary>
    /// Helper for building NotificationPayloadDto with calculated unread counter
    /// </summary>
    public static class NotificationPayloadHelper
    {
        /// <summary>
        /// Creates NotificationPayloadDto with calculated unread counter for the target user.
        /// NOTE: Counter includes the current notification (+1) because it will be persisted by QueuedNotificationSender
        /// </summary>
        public static async Task<NotificationPayloadDto> CreatePayloadAsync(
            NotificationDto notification,
            IReadRepository<Notification> notificationRepo,
            CancellationToken cancellationToken = default)
        {
            // Count existing unread notifications + 1 for this new one
            int unreadCount = await notificationRepo.CountAsync(
                n => n.UserId == notification.UserId && !n.Readed, 
                cancellationToken) + 1;

            return new NotificationPayloadDto(notification, unreadCount);
        }
    }
}
