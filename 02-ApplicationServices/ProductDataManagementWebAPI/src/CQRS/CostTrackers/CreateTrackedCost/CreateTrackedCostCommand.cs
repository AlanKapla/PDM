using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostTrackers;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostTrackers.CreateTrackedCost
{
    /// <summary>
    /// Command do tworzenia kosztu w trackerze
    /// </summary>
    public sealed record CreateTrackedCostCommand : IRequestCommand<TrackedCostWeb>, IAuthorizableRequest
    {
        public Guid? CostEstimateId { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public string? Contractor { get; init; }
        public DateTime? Date { get; init; }
        public IReadOnlyList<IFormFile>? NewFiles { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
