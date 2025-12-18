namespace CQRS.CostEstimates.GetCostEstimateTemplates
{
    /// <summary>
    /// Query do pobrania listy szablonów kosztorysów użytkownika
    /// </summary>
    public record GetCostEstimateTemplatesQuery : IRequestQuery<List<CostEstimateTemplateListItem>>;
    
    /// <summary>
    /// Result DTO for template list item
    /// </summary>
    public record CostEstimateTemplateListItem(
        Guid Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
