namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
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
        bool IsReadonly = false,
        bool SumInGroup = false,
        bool SumInTotal = false,
        List<FieldDefinitionDto>? ChildFields = null
    );

    public record UiConfigurationDto(List<Guid>? ColumnLayout);

    public record CategoryDto(
        string Name,
        string? Symbol,
        int Order
    );
}
