using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostTrackers;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostTrackers.Shared
{
    /// <summary>
    /// Bazowy record dla komend tworzenia i aktualizacji kosztu w trackerze.
    /// </summary>
    public abstract record TrackedCostCommandBase : CostTrackerCommandBase, IRequestCommand<TrackedCostWeb>
    {
        public required string Name { get; init; }
        public string? Number { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public Guid? ContractorId { get; init; }
        public Guid? CategoryId { get; init; }
        public DateTime? Date { get; init; }
        public IReadOnlyList<IFormFile>? NewFiles { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectDashboardTracker;
    }
}
