using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    /// <summary>
    /// Query to retrieve work schedules based on scope (All, Mine, Shared)
    /// </summary>
    public sealed record GetWorkSchedulesQuery(
        Guid TenantId,
        Guid ProjectId,
        ResourceScope Scope
    ) : IRequestQuery<List<WorkScheduleSummaryWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);

        public ResourceScope? GetResourceScope() => Scope;
    }
}
