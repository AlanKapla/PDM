using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed record AddWorkScheduleStageWorkCommand(
        Guid? CostEstimateItemId,
        string Name,
        int Order,
        string ColorRgb
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid WorkScheduleStageId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
