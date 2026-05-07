using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplates
{
    /// <summary>
    /// Query do pobrania listy szablonów kosztorysów użytkownika
    /// </summary>
    public record GetCostEstimateTemplatesQuery : IRequestQuery<List<CostEstimateTemplateListItemWeb>>;
}
