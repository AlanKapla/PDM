using MediatR;
using Entities.Models.CostEstimateTemplateDefinitions;

namespace CQRS.CostEstimates.UpdateCostEstimateTemplate
{
    /// <summary>
    /// Command do aktualizacji szablonu kosztorysu
    /// </summary>
    public record UpdateCostEstimateTemplateCommand(
        Guid TemplateId,
        string Name,
        string? Description,
        CostEstimateTemplateStructure TemplateStructure
    ) : IRequestCommand<Unit>;
}
