namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// List item DTO for default (system) cost estimate templates
    /// </summary>
    public record DefaultCostEstimateTemplateListItemWeb(
        string Slug,
        string Name,
        string? Description,
        string? Category
    );
}
