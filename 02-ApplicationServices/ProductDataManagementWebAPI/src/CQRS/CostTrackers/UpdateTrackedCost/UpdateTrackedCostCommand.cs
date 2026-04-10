using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostTrackers;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostTrackers.UpdateTrackedCost
{
    /// <summary>
    /// Command do aktualizacji kosztu w trackerze (pełne nadpisanie)
    /// </summary>
    public sealed record UpdateTrackedCostCommand : IRequestCommand<TrackedCostWeb>, IAuthorizableRequest
    {
        public Guid CostId { get; init; }
        public Guid? CostEstimateId { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public decimal? VatRate { get; init; }
        public string? Contractor { get; init; }
        public DateTime? Date { get; init; }
        public IReadOnlyList<IFormFile>? NewFiles { get; init; }
        public IReadOnlyList<Guid>? ExistingAttachmentIds { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
