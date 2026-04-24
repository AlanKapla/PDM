using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostTrackers;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostTrackers.Shared
{
    /// <summary>
    /// Bazowy record dla komend tworzenia i aktualizacji kosztu w trackerze
    /// </summary>
    public abstract record TrackedCostCommandBase : IRequestCommand<TrackedCostWeb>, IAuthorizableRequest
    {
        public required string Name { get; init; }
        public string? Number { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public string? Contractor { get; init; }
        public DateTime? Date { get; init; }
        public IReadOnlyList<IFormFile>? NewFiles { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
