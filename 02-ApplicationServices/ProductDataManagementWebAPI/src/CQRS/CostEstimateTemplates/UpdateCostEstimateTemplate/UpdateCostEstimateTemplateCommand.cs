using MediatR;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public record UpdateCostEstimateTemplateCommand(
        Guid TemplateId,
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
        List<FieldDefinitionDto>? GroupHeaderFields,
        List<FieldDefinitionDto>? SystemFields,
        List<FieldDefinitionDto>? CalculatedFields,
        List<FieldDefinitionDto>? GenericFields,
        UiConfigurationDto? UiConfiguration
    ) : IRequestCommand<Unit>;

    public record CurrencyDto(
        string Code,
        string Name,
        string? Symbol,
        bool IsDefault,
        int Order
    );

    public record UnitDto(
        string Code,
        string Name,
        string Symbol,
        string? Category,
        bool IsDefault,
        int Order
    );

    public record FieldDefinitionDto(
        Guid FieldName,
        int FieldType,
        string Label,
        bool IsSortable,
        bool IsFilterable,
        bool IsVisible = true,
        bool SumInGroup = false,
        bool SumInTotal = false,
        List<FieldDefinitionDto>? ChildFields = null
    );

    public record UiConfigurationDto(List<Guid>? ColumnLayout);
}
