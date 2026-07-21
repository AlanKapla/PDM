using Business.Interfaces.WebModels.Admin;
using CQRS;
using Entities.Enums;

namespace CQRS.Admin.ActivityLogs.GetUserActivityLogs
{
    public sealed record GetUserActivityLogsQuery(UserActivityEventType? EventType)
        : IRequestQuery<IReadOnlyList<UserActivityLogWeb>>;
}
