using Business.Interfaces.Model;
using Entities.Models.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkAllNotificationsAsRead
{
    public sealed class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
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
            int updated = await notificationRepo.ExecuteUpdateAsync(
                n => n.UserId == currentUser.Id && !n.IsRead,
                s => s.SetProperty(n => n.IsRead, true),
                cancellationToken);

            if (updated == 0)
            {
                logger.LogInformation("No unread notifications to mark as read for user {UserId}", currentUser.Id);
                return 0;
            }

            logger.LogInformation("Marked {Count} notifications as read for user {UserId}", updated, currentUser.Id);
            return updated;
        }
    }
}
