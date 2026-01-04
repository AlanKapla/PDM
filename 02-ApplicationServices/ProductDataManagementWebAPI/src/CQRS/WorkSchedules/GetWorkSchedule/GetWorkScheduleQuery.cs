using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.Interfaces;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    /// <summary>
    /// Query to retrieve a work schedule by its ID with full details
    /// </summary>
    public sealed record GetWorkScheduleQuery(
        Guid TenantId,
        Guid ProjectId,
        Guid WorkScheduleId
    ) : IRequestQuery<WorkScheduleDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesRead;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
