using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.AddWorkScheduleStage
{
    public sealed record AddWorkScheduleStageCommand : WorkScheduleCommandBase, IRequestCommand<Guid>
    {
        public Guid? ParentStageId { get; init; }
        public Guid? CostEstimateGroupId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
