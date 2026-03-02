using MediatR;

namespace CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate
{
    /// <summary>
    /// Command to soft-delete a cost estimate template
    /// </summary>
    public sealed record DeleteCostEstimateTemplateCommand(
        Guid TemplateId
    ) : IRequestCommand<Unit>;
}
