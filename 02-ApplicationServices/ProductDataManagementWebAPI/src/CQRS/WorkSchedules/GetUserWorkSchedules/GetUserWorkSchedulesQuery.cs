using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.Interfaces;

namespace CQRS.WorkSchedules.GetUserWorkSchedules
{
    /// <summary>
    /// Query to retrieve work schedules created by the current user
    /// </summary>
    public sealed record GetUserWorkSchedulesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<WorkScheduleSummaryWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
