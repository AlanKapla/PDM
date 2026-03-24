using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public record UpdateCostEstimateTemplateCommand(
        Guid TemplateId,
        Guid CurrentVersionId,
        string Name,
        string? Description,
        string? Category,
        bool CanAddGroups,
        bool CanBranchGroups,
        int? MaxGroupLevel,
        bool AutoNumberGroups,
        string? GroupNumberFormat,
        bool UpdateStructure,
        List<CurrencyDto>? Currencies,
        List<UnitDto>? Units,
        List<CategoryDto>? Categories,
        List<FieldDefinitionDto>? GroupHeaderFields,
        List<FieldDefinitionDto>? SystemFields,
        List<FieldDefinitionDto>? CalculatedFields,
        List<FieldDefinitionDto>? GenericFields,
        UiConfigurationDto? UiConfiguration
    ) : IRequestCommand<Unit>;
}
