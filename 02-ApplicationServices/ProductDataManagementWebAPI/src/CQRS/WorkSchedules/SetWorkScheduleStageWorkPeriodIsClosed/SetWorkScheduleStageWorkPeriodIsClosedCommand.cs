using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed record SetWorkScheduleStageWorkPeriodIsClosedCommand(bool IsClosed)
        : IRequestCommand<Unit>, IAssignedAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid WorkScheduleStageWorkId { get; init; }
        public Guid PeriodId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWriteOwn;
    }
}
