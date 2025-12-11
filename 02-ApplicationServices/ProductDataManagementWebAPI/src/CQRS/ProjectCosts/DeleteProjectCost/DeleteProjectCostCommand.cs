using MediatR;

namespace CQRS.ProjectCosts.DeleteProjectCost
{
    /// <summary>
    /// Command do usunięcia kosztu projektu (soft delete)
    /// </summary>
    public record DeleteProjectCostCommand : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostId { get; init; }
    }
}
