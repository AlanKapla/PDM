using Business.Interfaces.Model;
using Entities.Models;
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
                n => n.UserId == currentUser.Id && !n.Readed);

            if (!unreadNotifications.Any())
            {
                logger.LogInformation("No unread notifications to mark as read for user {UserId}", currentUser.Id);
                return 0;
            }

            foreach (var notification in unreadNotifications)
            {
                notification.Readed = true;
            }

            await notificationRepo.UpdateRange(unreadNotifications);

            logger.LogInformation("Marked {Count} notifications as read for user {UserId}", 
                unreadNotifications.Count(), currentUser.Id);

            return unreadNotifications.Count();
        }
    }
}
