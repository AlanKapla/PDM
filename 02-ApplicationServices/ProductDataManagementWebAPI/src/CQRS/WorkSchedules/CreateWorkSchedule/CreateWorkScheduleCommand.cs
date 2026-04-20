using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed record CreateWorkScheduleCommand(
        string Name,
        Guid? CostEstimateId
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
