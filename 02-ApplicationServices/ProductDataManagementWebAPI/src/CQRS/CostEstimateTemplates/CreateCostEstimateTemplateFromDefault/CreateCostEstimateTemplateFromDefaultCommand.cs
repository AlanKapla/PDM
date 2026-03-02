namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault
{
    /// <summary>
    /// Creates a new template with full structure copied from a default (system) template.
    /// New fieldName GUIDs are generated server-side.
    /// </summary>
    public record CreateCostEstimateTemplateFromDefaultCommand(
        string Slug,
        string Name,
        string? Description
    ) : IRequestCommand<Guid>;
}
