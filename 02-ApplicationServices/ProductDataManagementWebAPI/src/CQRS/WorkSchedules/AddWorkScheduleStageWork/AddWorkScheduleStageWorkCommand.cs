using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed record AddWorkScheduleStageWorkCommand : WorkScheduleStageCommandBase, IRequestCommand<Guid>
    {
        public Guid? CostEstimateItemId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
        public string ColorRgb { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
