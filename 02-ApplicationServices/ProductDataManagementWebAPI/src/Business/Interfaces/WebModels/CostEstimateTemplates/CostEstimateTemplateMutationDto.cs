namespace Business.Interfaces.WebModels.CostEstimateTemplates;

/// <summary>
/// DTO for Currency definition in template
/// </summary>
public record CurrencyDto(
    string Code,
    string Name,
    string? Symbol,
    bool IsDefault,
    int Order
);

/// <summary>
/// DTO for Unit definition in template
/// </summary>
public record UnitDto(
    string Code,
    string Name,
    string Symbol,
    string? Category,
    bool IsDefault,
    int Order
);

/// <summary>
/// DTO for Field definition in template
/// </summary>
public record FieldDefinitionDto(
    Guid FieldName,         // Unique identifier for this field (NOT FieldDefinitionId!)
    int FieldType,          // FieldType enum value (0-9 Group, 100-199 System, 200-299 Calculated, 300-399 Generic)
    string Label,
    bool IsSortable,
    bool IsFilterable,
    bool IsVisible,
    bool SumInGroup = false,   // Only for calculated fields (203, 204, 206)
    bool SumInTotal = false,   // Only for calculated fields (203, 204, 206)
    List<FieldDefinitionDto>? ChildFields = null  // Only for ItemSystemOptions (103)
);

/// <summary>
/// DTO for UI Configuration (column layout)
/// </summary>
public record UiConfigurationDto(
    List<Guid>? ColumnLayout  // Optional list of FieldName GUIDs in display order
);

/// <summary>
/// DTO for updating CostEstimateTemplate structure
/// Used by services instead of Commands to avoid circular dependency
/// </summary>
public record CostEstimateTemplateUpdateDto(
    Guid TemplateId,
    string Name,
    string? Description,
    string? Category,
    bool CanAddGroups,
    bool CanBranchGroups,
    int? MaxGroupLevel,
    bool AutoNumberGroups,
    string? GroupNumberFormat,
    bool UpdateStructure,  // If true, updates field definitions
    List<CurrencyDto>? Currencies,
    List<UnitDto>? Units,
    List<FieldDefinitionDto>? GroupHeaderFields,
    List<FieldDefinitionDto>? SystemFields,
    List<FieldDefinitionDto>? CalculatedFields,
    List<FieldDefinitionDto>? GenericFields,
    UiConfigurationDto? UiConfiguration
);
