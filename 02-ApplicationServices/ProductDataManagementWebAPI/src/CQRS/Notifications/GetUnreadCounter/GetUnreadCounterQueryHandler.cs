using Business.Interfaces.Model;
using Entities.Models.Notifications;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.GetUnreadCounter
{
    public sealed class GetUnreadCounterQueryHandler : IRequestHandler<GetUnreadCounterQuery, int>
    {
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;

        public GetUnreadCounterQueryHandler(IReadRepository<Notification> notificationRepo, ICurrentUser currentUser)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
        }

        public async Task<int> Handle(GetUnreadCounterQuery request, CancellationToken cancellationToken)
        {
            int count = await notificationRepo.CountAsync(n => n.UserId == currentUser.Id && !n.IsRead, cancellationToken);
            return count;
        }
    }
}
