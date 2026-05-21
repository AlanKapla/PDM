using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using Entities.Models.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.GetUnreadNotifications
{
    public sealed class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, IEnumerable<NotificationWeb>>
    {
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;

        public GetUnreadNotificationsQueryHandler(IReadRepository<Notification> notificationRepo, ICurrentUser currentUser)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<NotificationWeb>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            List<Notification> notifications = await notificationRepo.GetPagedBySearchAsync(
                n => n.UserId == currentUser.Id && !n.IsRead,
                n => n.CreatedAt,
                descending: true,
                request.Skip,
                request.Take,
                cancellationToken,
                include => include.Include(n => n.Tenant).Include(n => n.Project));

            List<NotificationWeb> items = notifications
                .Select(NotificationWebMapper.ToWeb)
                .ToList();

            return items;
        }
    }
}
