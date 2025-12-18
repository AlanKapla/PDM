using Entities.Models;
using Entities.Models.CostEstimateData;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Command do aktualizacji wypełnionego kosztorysu
    /// </summary>
    public record UpdateCostEstimateCommand(
        string Name,
        string? Description,
        CostEstimateStatus Status,
        CostEstimateDataModel Data,
        decimal? TotalNet,
        decimal? TotalGross
    ) : IRequestCommand<Unit>
    {
        public Guid CostEstimateId { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
