using Entities.Models;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Result DTO for template list item
    /// </summary>
    public record CostEstimateTemplateListItemWeb(
        Guid Id,
        string Name,
        string? Description,
        string? Category,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
