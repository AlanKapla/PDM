using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using Entities.Models.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.GetAllNotifications
{
    public sealed class GetAllNotificationsQueryHandler : IRequestHandler<GetAllNotificationsQuery, IEnumerable<NotificationWeb>>
    {
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<GetAllNotificationsQueryHandler> logger;

        public GetAllNotificationsQueryHandler(
            IReadRepository<Notification> notificationRepo,
            ICurrentUser currentUser,
            ILogger<GetAllNotificationsQueryHandler> logger)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<IEnumerable<NotificationWeb>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all notifications for user {UserId}, take={Take}, skip={Skip}", currentUser.Id, request.Take, request.Skip);

            List<Notification> notifications = await notificationRepo.GetPagedBySearchAsync(
                n => n.UserId == currentUser.Id,
                n => n.CreatedAt,
                descending: true,
                request.Skip,
                request.Take,
                cancellationToken,
                include => include.Include(n => n.Tenant).Include(n => n.Project));

            List<NotificationWeb> items = notifications
                .Select(NotificationWebMapper.ToWeb)
                .ToList();

            logger.LogInformation("Returning {Count} notifications for user {UserId}", items.Count, currentUser.Id);
            return items;
        }
    }
}
