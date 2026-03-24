namespace CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate
{
    /// <summary>
    /// Command to duplicate an existing cost estimate template with all its structure
    /// </summary>
    public sealed record DuplicateCostEstimateTemplateCommand(
        Guid SourceTemplateId,
        string Name,
        string? Description
    ) : IRequestCommand<Guid>;
}
