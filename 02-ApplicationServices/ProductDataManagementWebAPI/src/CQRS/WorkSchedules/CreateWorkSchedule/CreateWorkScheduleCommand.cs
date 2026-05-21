using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed record CreateWorkScheduleCommand : WorkScheduleRequestBase, IRequestCommand<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public Guid? CostEstimateId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
