using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

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
