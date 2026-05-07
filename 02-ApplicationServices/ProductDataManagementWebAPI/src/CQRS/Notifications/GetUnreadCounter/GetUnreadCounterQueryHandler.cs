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
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.GetUnreadCounter
{
    public class GetUnreadCounterQueryHandler : IRequestHandler<GetUnreadCounterQuery, int>
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
