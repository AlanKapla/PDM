using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models.Activity;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Activity.RecordLoginActivity
{
    public sealed class RecordLoginActivityCommandHandler
        : IRequestHandler<RecordLoginActivityCommand, Unit>
    {
        private readonly IRepository<UserActivityLog> activityLogRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public RecordLoginActivityCommandHandler(
            IRepository<UserActivityLog> activityLogRepo,
            IReadRepository<User> userRepo,
            ICurrentUser currentUser)
        {
            this.activityLogRepo = activityLogRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(
            RecordLoginActivityCommand request,
            CancellationToken cancellationToken)
        {
            string? azureAdB2CObjectId = ResolveAzureAdB2CObjectId();
            Guid? userId = await ResolveUserIdAsync(azureAdB2CObjectId, cancellationToken);

            UserActivityLog log = new()
            {
                EventType = UserActivityEventType.Login,
                IpAddress = request.IpAddress,
                OccurredAtUtc = DateTime.UtcNow,
                Route = request.Route,
                UserId = userId,
                AzureAdB2CObjectId = azureAdB2CObjectId
            };

            await activityLogRepo.Insert(log);
            await activityLogRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private string? ResolveAzureAdB2CObjectId()
        {
            string? oid = currentUser.AzureAdB2CObjectId;
            if (string.IsNullOrWhiteSpace(oid))
            {
                oid = currentUser.GetClaimValue("oid");
            }

            if (string.IsNullOrWhiteSpace(oid))
            {
                return null;
            }

            return oid;
        }

        private async Task<Guid?> ResolveUserIdAsync(
            string? azureAdB2CObjectId,
            CancellationToken cancellationToken)
        {
            if (azureAdB2CObjectId is null)
            {
                return null;
            }

            User? user = await userRepo.GetFirstBySearch(
                u => u.AzureAdB2CObjectId == azureAdB2CObjectId,
                cancellationToken);

            if (user is null)
            {
                return null;
            }

            return user.Id;
        }
    }
}
