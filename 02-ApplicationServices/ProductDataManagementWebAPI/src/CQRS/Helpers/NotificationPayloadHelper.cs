using Business.Interfaces.DTO;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
                n => n.UserId == notification.UserId && !n.IsRead, 
                cancellationToken) + 1;

            return new NotificationPayloadDto(notification, unreadCount);
        }
    }
}
