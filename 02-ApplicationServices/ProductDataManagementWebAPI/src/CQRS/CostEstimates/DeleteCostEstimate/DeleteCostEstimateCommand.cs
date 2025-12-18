using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Command do usunięcia kosztorysu (soft delete)
    /// </summary>
    public record DeleteCostEstimateCommand(
        Guid CostEstimateId
    ) : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
