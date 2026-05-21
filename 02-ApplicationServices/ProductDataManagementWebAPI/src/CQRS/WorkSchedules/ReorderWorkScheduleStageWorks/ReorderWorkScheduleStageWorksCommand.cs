using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed record ReorderWorkScheduleStageWorksCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public List<Guid> OrderedWorkIds { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
