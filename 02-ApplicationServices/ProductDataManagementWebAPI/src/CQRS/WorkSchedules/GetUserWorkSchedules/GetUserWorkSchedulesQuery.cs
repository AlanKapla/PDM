using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetUserWorkSchedules
{
    /// <summary>
    /// Query to retrieve work schedules created by the current user
    /// </summary>
    public record GetUserWorkSchedulesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<WorkScheduleSummaryWeb>>;
}
