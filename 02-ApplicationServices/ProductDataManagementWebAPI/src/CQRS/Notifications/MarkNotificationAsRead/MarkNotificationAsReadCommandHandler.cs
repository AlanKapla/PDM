using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
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
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
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
            // Walidacja wykonana w validatorze - notyfikacja istnieje i należy do użytkownika
            Notification notification = (await notificationRepo
                .GetFirstBySearch(n => n.Id == request.NotificationId && n.UserId == currentUser.Id))!;

            // Jeśli już przeczytana, nie rób nic
            if (notification.IsRead)
            {
                return Unit.Value;
            }

            // Oznacz jako przeczytaną
            notification.IsRead = true;
            await notificationRepo.Update(notification);

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
