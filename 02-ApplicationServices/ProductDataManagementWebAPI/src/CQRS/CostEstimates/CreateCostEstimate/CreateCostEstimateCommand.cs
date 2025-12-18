using Entities.Models.CostEstimateData;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia pustego kosztorysu na podstawie szablonu
    /// Dane będą wypełniane później przez Update
    /// </summary>
    public record CreateCostEstimateCommand(
        Guid TemplateId,
        string Name,
        string? Description
    ) : IRequestCommand<Guid>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
