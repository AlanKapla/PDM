using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    /// <summary>
    /// Query to retrieve work schedules based on scope (All, Mine, Shared)
    /// </summary>
    public sealed record GetWorkSchedulesQuery : WorkScheduleRequestBase, IRequestQuery<List<WorkScheduleSummaryWeb>>
    {
        public ResourceScope Scope { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectView;

        public ResourceScope? GetResourceScope() => Scope;
    }
}
