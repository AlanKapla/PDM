using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetTemplateVersionStructure
{
    /// <summary>
    /// Query do pobrania pełnej struktury wersji szablonu kosztorysu
    /// Zwraca wszystkie dane potrzebne do utworzenia kosztorysu
    /// </summary>
    public record GetTemplateVersionStructureQuery(
        Guid TemplateId,
        Guid VersionId
    ) : IRequestQuery<CostEstimateTemplateVersionStructureWeb>;
}
