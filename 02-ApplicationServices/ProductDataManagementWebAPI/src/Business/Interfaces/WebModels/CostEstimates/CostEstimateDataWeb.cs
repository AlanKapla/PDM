namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Wartość pola w kosztorysie (wspólna dla grup i pozycji)
    /// Wartość zwracana w odpowiednim polu typowanym w zależności od FieldType
    /// </summary>
    public record CostEstimateFieldValueWeb(
        Guid Id,
        Guid FieldDefinitionId,
        int FieldType,      // FieldType enum jako int (kompatybilność JSON)
        int FieldScope,     // FieldScope enum jako int (Group/ItemSystem/ItemCalculated/ItemGeneric)
        Guid? FieldName,    // GUID pola (dla pozycji)
        string? FieldLabel,
        string? StringValue,
        decimal? DecimalValue,
        bool? BoolValue,
        DateTime? DateTimeValue
    );
    
    /// <summary>
    /// Pozycja kosztorysu (work scope item)
    /// Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
    /// Może zawierać kolekcję Components - pozycja składa się z komponentów
    /// WAŻNE: Options i Components mogą mieć tylko 1 poziom zagnieżdżenia (child nie może mieć childa)
    /// </summary>
    public record CostEstimateItemWeb(
        Guid Id,
        Guid GroupId,
        Guid? ParentItemId,     // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
        int RelationType,        // ItemRelationType jako int: None=0, Option=1, Component=2
        int Order,
        decimal? NetValue,       // Obliczona wartość netto (z komponentów lub pól)
        decimal? GrossValue,     // Obliczona wartość brutto
        decimal? VatValue,       // Obliczona wartość VAT
        List<CostEstimateFieldValueWeb> FieldValues,
        List<CostEstimateItemWeb>? Options,      // Kolekcja opcji - jeśli ParentItemId != null, to lista będzie pusta
        List<CostEstimateItemWeb>? Components,   // Kolekcja komponentów - jeśli ParentItemId != null, to lista będzie pusta
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
        List<CostEstimateFieldValueWeb> FieldValues,
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
