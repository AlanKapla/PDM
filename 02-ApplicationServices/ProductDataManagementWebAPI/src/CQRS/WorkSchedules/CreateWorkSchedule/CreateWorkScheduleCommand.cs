using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed record CreateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name,
        Guid? CostEstimateId,
        List<WorkScheduleStageDto>? Stages,
        List<WorkScheduleWorkDependencyDto>? Dependencies
    ) : IRequestCommand<WorkScheduleDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
