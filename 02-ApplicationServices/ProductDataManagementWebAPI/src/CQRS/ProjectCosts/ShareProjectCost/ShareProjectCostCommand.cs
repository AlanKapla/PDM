using MediatR;

namespace CQRS.ProjectCosts.ShareProjectCost
{
    /// <summary>
    /// Command do udostępnienia kosztu członkom projektu
    /// </summary>
    public record ShareProjectCostCommand : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostId { get; init; }
        public List<Guid> SharedWithUserIds { get; init; } = new();
    }
}
