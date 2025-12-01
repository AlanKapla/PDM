using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
    {
        private readonly IRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;

        public MarkNotificationAsReadCommandHandler(IRepository<Notification> notificationRepo, ICurrentUser currentUser)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            // Pobierz notyfikacjê nale¿¹c¹ do zalogowanego u¿ytkownika
            Notification? notification = await notificationRepo
                .GetFirstBySearch(n => n.Id == request.NotificationId && n.UserId == currentUser.Id);

            if (notification == null)
            {
                throw new NotFoundApiException(nameof(Notification), request.NotificationId.ToString());
            }

            // Jeœli ju¿ przeczytana, nie rób nic
            if (notification.Readed)
            {
                return Unit.Value;
            }

            // Oznacz jako przeczytan¹
            notification.Readed = true;
            await notificationRepo.Update(notification);

            return Unit.Value;
        }
    }
}
