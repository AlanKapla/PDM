using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectDashboard;

namespace CQRS.ProjectDashboard.GetProjectDashboard
{
    /// <summary>
    /// Query to get full cost tracker details for a project (all estimates aggregated)
    /// </summary>
    public sealed record GetProjectDashboardQuery : IRequestQuery<ProjectDashboardWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }

        public required Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
