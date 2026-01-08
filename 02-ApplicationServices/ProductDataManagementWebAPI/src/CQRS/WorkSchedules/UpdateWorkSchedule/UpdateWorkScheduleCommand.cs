using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public sealed record UpdateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid WorkScheduleId,
        string Name,
        List<WorkScheduleStageDto>? Stages
    ) : IRequestCommand<WorkScheduleDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
