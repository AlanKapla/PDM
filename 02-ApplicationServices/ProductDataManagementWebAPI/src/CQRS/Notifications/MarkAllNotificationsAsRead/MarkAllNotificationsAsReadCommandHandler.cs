using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
    {
        private readonly IRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<MarkAllNotificationsAsReadCommandHandler> logger;

        public MarkAllNotificationsAsReadCommandHandler(
            IRepository<Notification> notificationRepo,
            ICurrentUser currentUser,
            ILogger<MarkAllNotificationsAsReadCommandHandler> logger)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<int> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var unreadNotifications = await notificationRepo.GetBySearch(
                n => n.UserId == currentUser.Id && !n.IsRead);

            if (!unreadNotifications.Any())
            {
                logger.LogInformation("No unread notifications to mark as read for user {UserId}", currentUser.Id);
                return 0;
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await notificationRepo.UpdateRange(unreadNotifications);

            logger.LogInformation("Marked {Count} notifications as read for user {UserId}", 
                unreadNotifications.Count(), currentUser.Id);

            return unreadNotifications.Count();
        }
    }
}
