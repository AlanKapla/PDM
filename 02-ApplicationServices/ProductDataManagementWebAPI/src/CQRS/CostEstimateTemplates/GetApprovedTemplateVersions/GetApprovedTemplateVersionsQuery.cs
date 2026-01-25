using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetApprovedTemplateVersions
{
    /// <summary>
    /// Query do pobrania wszystkich zatwierdzonych wersji szablonów
    /// Zwraca zatwierdzone wersje dla wszystkich szablonów (do wyboru przy tworzeniu kosztorysu)
    /// </summary>
    public record GetApprovedTemplateVersionsQuery : IRequestQuery<List<ApprovedTemplateVersionItemWeb>>;
}
