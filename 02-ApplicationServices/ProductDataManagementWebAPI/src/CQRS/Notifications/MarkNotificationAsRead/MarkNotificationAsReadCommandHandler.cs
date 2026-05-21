using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Notifications;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public sealed class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
    {
        private readonly IRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;
        private readonly INotificationMarkAsReadSender notificationMarkAsReadSender;

        public MarkNotificationAsReadCommandHandler(
            IRepository<Notification> notificationRepo,
            ICurrentUser currentUser,
            INotificationMarkAsReadSender notificationMarkAsReadSender)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
            this.notificationMarkAsReadSender = notificationMarkAsReadSender;
        }

        public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            Notification? notification = await notificationRepo
                .GetFirstBySearch(n => n.Id == request.NotificationId && n.UserId == currentUser.Id);

            if (notification is null)
            {
                throw new NotFoundApiException(nameof(Notification), request.NotificationId.ToString());
            }

            if (notification.IsRead)
            {
                return Unit.Value;
            }

            notification.IsRead = true;
            await notificationRepo.Update(notification);
            await notificationRepo.SaveChangesAsync(cancellationToken);

            await notificationMarkAsReadSender.EnqueueAsync(new NotificationMarkAsReadDto
            {
                NotificationId = notification.Id,
                UserId = notification.UserId,
                AzureAdB2CObjectId = currentUser.AzureAdB2CObjectId,
                ReadAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            return Unit.Value;
        }
    }
}
