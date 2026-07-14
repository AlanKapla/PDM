using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStages
{
    public sealed record ReorderWorkScheduleStagesCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public List<Guid> OrderedStageIds { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
