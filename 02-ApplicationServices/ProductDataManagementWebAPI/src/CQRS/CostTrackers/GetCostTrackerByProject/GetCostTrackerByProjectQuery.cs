using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostTrackers;

namespace CQRS.CostTrackers.GetCostTrackerByProject
{
    /// <summary>
    /// Query to get full cost tracker details for a project (all estimates aggregated)
    /// </summary>
    public sealed record GetCostTrackerByProjectQuery() : IRequestQuery<CostTrackerDetailsWeb>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }

        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
