using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using Entities.Enums;
using Entities.Models.Activity;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Admin.ActivityLogs.GetUserActivityLogs
{
    public sealed class GetUserActivityLogsQueryHandler
        : IRequestHandler<GetUserActivityLogsQuery, IReadOnlyList<UserActivityLogWeb>>
    {
        private const int MaxResults = 500;

        private readonly IReadRepository<UserActivityLog> activityLogRepo;
        private readonly ICurrentUser currentUser;

        public GetUserActivityLogsQueryHandler(
            IReadRepository<UserActivityLog> activityLogRepo,
            ICurrentUser currentUser)
        {
            this.activityLogRepo = activityLogRepo;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyList<UserActivityLogWeb>> Handle(
            GetUserActivityLogsQuery request,
            CancellationToken cancellationToken)
        {
            EnsureSuperAdmin();

            Expression<Func<UserActivityLog, bool>> predicate = BuildPredicate(request.EventType);

            List<UserActivityLog> logs = await activityLogRepo.GetPagedBySearchAsync(
                predicate,
                l => l.OccurredAtUtc,
                descending: true,
                skip: 0,
                take: MaxResults,
                cancellationToken);

            return logs.Select(MapToWeb).ToList();
        }

        private void EnsureSuperAdmin()
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can view user activity logs.");
            }
        }

        private static Expression<Func<UserActivityLog, bool>> BuildPredicate(
            UserActivityEventType? eventType)
        {
            if (eventType is null)
            {
                return _ => true;
            }

            UserActivityEventType filter = eventType.Value;
            return l => l.EventType == filter;
        }

        private static UserActivityLogWeb MapToWeb(UserActivityLog log)
        {
            return new UserActivityLogWeb(
                Id: log.Id,
                EventType: log.EventType.ToString(),
                IpAddress: log.IpAddress,
                OccurredAtUtc: log.OccurredAtUtc,
                Route: log.Route,
                UserId: log.UserId,
                AzureAdB2CObjectId: log.AzureAdB2CObjectId);
        }
    }
}
