using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    /// <summary>
    /// Query to retrieve a work schedule by its ID with full details
    /// </summary>
    public record GetWorkScheduleQuery(
        Guid TenantId,
        Guid ProjectId,
        Guid WorkScheduleId
    ) : IRequestQuery<WorkScheduleDetailsWeb>;
}
