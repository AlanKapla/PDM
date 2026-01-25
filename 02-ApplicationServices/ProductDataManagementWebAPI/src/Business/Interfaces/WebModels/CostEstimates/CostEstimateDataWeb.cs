namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Wartość pola grupy kosztorysu
    /// </summary>
    public record CostEstimateGroupFieldValueWeb(
        Guid Id,
        Guid FieldDefinitionId,
        int FieldType,      // FieldType enum jako int (kompatybilność JSON)
        int FieldScope,     // FieldScope enum jako int (zawsze Group dla grupy)
        string? FieldLabel,
        string? Value
    );
    
    /// <summary>
    /// Wartość pola pozycji kosztorysu
    /// Używa pojedynczego FieldDefinitionId wskazującego na CostEstimateTemplateFieldDefinitionBase
    /// </summary>
    public record CostEstimateItemFieldValueWeb(
        Guid Id,
        Guid FieldDefinitionId,
        int FieldType,      // FieldType enum jako int (kompatybilność JSON)
        int FieldScope,     // FieldScope enum jako int (ItemSystem/ItemCalculated/ItemGeneric)
        Guid? FieldName,
        string? FieldLabel,
        string? Value
    );
    
    /// <summary>
    /// Pozycja kosztorysu (work scope item)
    /// Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
    /// </summary>
    public record CostEstimateItemWeb(
        Guid Id,
        Guid GroupId,
        Guid? ParentItemId,     // ID pozycji nadrzędnej (jeśli to opcja)
        int Order,
        List<CostEstimateItemFieldValueWeb> FieldValues,
        List<CostEstimateItemWeb>? Options,  // Kolekcja opcji (zagnieżdżonych pozycji)
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
    
    /// <summary>
    /// Grupa kosztorysu
    /// </summary>
    public record CostEstimateGroupWeb(
        Guid Id,
        Guid? ParentGroupId,
        int Level,
        int Order,
        List<CostEstimateGroupFieldValueWeb> FieldValues,
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        DateTime? LastCalculatedAt,
        List<CostEstimateGroupWeb> ChildGroups,
        List<CostEstimateItemWeb> Items,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
