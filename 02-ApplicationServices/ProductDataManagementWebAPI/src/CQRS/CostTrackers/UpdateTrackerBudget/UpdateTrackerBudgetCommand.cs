using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostTrackers.UpdateTrackerBudget
{
    /// <summary>
    /// Command do aktualizacji pól budżetowych (BudgetNet, BudgetGross) w trackerze kosztów
    /// </summary>
    public sealed record UpdateTrackerBudgetCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid CostTrackerId { get; init; }
        public decimal? BudgetNet { get; init; }
        public decimal? BudgetGross { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
