using Entities.Models.CostEstimateTemplateDefinitions;

namespace CQRS.CostEstimates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Command do tworzenia szablonu kosztorysu
    /// </summary>
    public record CreateCostEstimateTemplateCommand(
        string Name,
        string? Description,
        CostEstimateTemplateStructure TemplateStructure
    ) : IRequestCommand<Guid>;
}
