using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods
{
    public sealed record SetWorkScheduleStageWorkPeriodsCommand(
        List<WorkPeriodDto> Periods
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid WorkScheduleStageWorkId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
