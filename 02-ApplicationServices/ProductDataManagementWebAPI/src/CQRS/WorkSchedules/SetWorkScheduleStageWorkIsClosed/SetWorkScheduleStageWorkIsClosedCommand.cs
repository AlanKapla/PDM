using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed record SetWorkScheduleStageWorkIsClosedCommand(bool IsClosed)
        : IRequestCommand<Unit>, IAssignedAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid WorkScheduleStageWorkId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWriteOwn;
    }
}
