using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimateTemplate
{
    /// <summary>
    /// Command do usunięcia szablonu kosztorysu (soft delete)
    /// </summary>
    public record DeleteCostEstimateTemplateCommand(
        Guid TemplateId
    ) : IRequestCommand<Unit>;
}
