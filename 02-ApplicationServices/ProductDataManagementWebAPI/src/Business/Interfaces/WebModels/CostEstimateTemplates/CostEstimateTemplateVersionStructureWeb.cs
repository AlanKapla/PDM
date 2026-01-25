using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    public record CostEstimateTemplateVersionStructureWeb(
        Guid VersionId,
        int VersionNumber,
        string? VersionName,
        List<CurrencyWeb> Currencies,
        List<UnitWeb> Units,
        List<FieldDefinitionWeb> GroupHeaderFields,
        List<FieldDefinitionWeb> SystemFields,
        List<FieldDefinitionWeb> CalculatedFields,
        List<FieldDefinitionWeb> GenericFields,
        SummaryConfigurationWeb? SummaryConfiguration,
        UiConfigurationWeb? UiConfiguration
    );
    
    public record CurrencyWeb(
        Guid Id,
        string Code,
        string Name,
        string? Symbol,
        bool IsDefault,
        int Order
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
        CostEstimateFieldTypeConfigWeb FieldTypeConfig,
        List<FieldDefinitionWeb>? ChildFields = null
    );
    
    public record SummaryConfigurationWeb(
        bool ShowGroupSummary,
        bool ShowTotalSummary,
        List<SummaryFieldWeb> GroupSummaryFields,
        List<SummaryFieldWeb> TotalSummaryFields
    );
    
    /// <summary>
    /// Pole do sumowania w konfiguracji podsumowania
    /// FieldType i FieldScope są int dla kompatybilności JSON/HTTP
    /// Wartości odpowiadają enumom FieldType i FieldScope z Entities.Models.CostEstimates
    /// </summary>
    public record SummaryFieldWeb(
        Guid FieldId,
        Guid FieldName,
        int FieldType,
        string FieldLabel,
        int FieldScope,
        int Order
    );
    
    public record UiConfigurationWeb(
        List<ColumnConfigurationWeb> Columns
    );
    
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
        bool IsVisible,
        string? Width
    );
}
