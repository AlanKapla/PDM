using Entities.Enums;
using Entities.Models.Activity;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Activity.RecordDemoActivity
{
    public sealed class RecordDemoActivityCommandHandler
        : IRequestHandler<RecordDemoActivityCommand, Unit>
    {
        private readonly IRepository<UserActivityLog> activityLogRepo;

        public RecordDemoActivityCommandHandler(IRepository<UserActivityLog> activityLogRepo)
        {
            this.activityLogRepo = activityLogRepo;
        }

        public async Task<Unit> Handle(
            RecordDemoActivityCommand request,
            CancellationToken cancellationToken)
        {
            UserActivityLog log = new()
            {
                EventType = UserActivityEventType.DemoEnter,
                IpAddress = request.IpAddress,
                OccurredAtUtc = DateTime.UtcNow,
                Route = request.Route,
                UserId = null,
                AzureAdB2CObjectId = null
            };

            await activityLogRepo.Insert(log);
            await activityLogRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
