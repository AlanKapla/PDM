namespace CQRS.CostEstimateTemplates.Shared
{
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
        List<FieldDefinitionDto>? ChildFields = null
    );

    public record SummaryConfigurationDto(
        bool ShowGroupSummary,
        bool ShowTotalSummary,
        List<Guid> GroupSummaryFields,  
        List<Guid> TotalSummaryFields   
    );

    public record UiConfigurationDto(
        List<Guid>? ColumnLayout,  
        Dictionary<Guid, string>? ColumnWidths  
    );
}
