using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.SetWorkScheduleDependencies
{
    public sealed record SetWorkScheduleDependenciesCommand : WorkScheduleCommandBase, IRequestCommand<WorkScheduleDetailsWeb>
    {
        public List<WorkDependencyDto> Dependencies { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
