using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    public record CostEstimateTemplateStructureWeb(
        Guid TemplateId,
        int? MaxGroupLevel,
        List<UnitWeb> Units,
        List<CategoryWeb> Categories,
        List<FieldDefinitionWeb> GroupHeaderFields,
        List<FieldDefinitionWeb> SystemFields,
        List<FieldDefinitionWeb> CalculatedFields,
        List<FieldDefinitionWeb> GenericFields,
        UiConfigurationWeb? UiConfiguration
    );

    public record UnitWeb(
        Guid Id,
        string Code,
        string Name,
        string Symbol,
        string? Category,
        bool IsDefault,
        int Order
    );

    public record CategoryWeb(
        Guid Id,
        string Name,
        string? Symbol,
        int Order
    );
    
    /// <summary>
    /// Definicja pola w szablonie kosztorysu
    /// FieldType i FieldScope są dostępne w FieldTypeConfig
    /// </summary>
    public record FieldDefinitionWeb(
        Guid Id,
        Guid FieldName,
        string Label,
        bool IsSortable,
        bool IsFilterable,
        bool IsVisible,
        bool IsReadonly,
        CostEstimateFieldTypeConfigWeb FieldTypeConfig,
        bool SumInGroup = false,
        bool SumInTotal = false,
        List<FieldDefinitionWeb>? ChildFields = null
    );
    
    public record UiConfigurationWeb(List<ColumnConfigurationWeb> Columns);
    
    /// <summary>
    /// Konfiguracja kolumny w UI
    /// FieldType i FieldScope są int dla kompatybilności JSON/HTTP
    /// Wartości odpowiadają enumom FieldType i FieldScope z Entities.Models.CostEstimates
    /// </summary>
    public record ColumnConfigurationWeb(
        Guid FieldId,
        Guid FieldName,
        int FieldType,
        string FieldLabel,
        int FieldScope,
        int Order,
        bool IsVisible = true
    );
}
