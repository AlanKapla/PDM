namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Pozycja kosztorysu (work scope item)
    /// Może zawierać kolekcję Options (warianty)
    /// Może zawierać kolekcję Components (składniki pozycji)
    /// WAŻNE: Options i Components mogą mieć tylko 1 poziom zagnieżdżenia (child nie może mieć childa)
    /// </summary>
    public sealed record CostEstimateItemWeb(
        Guid Id,
        Guid GroupId,
        Guid? ParentItemId,     // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
        int RelationType,        // ItemRelationType jako int: None=0, Option=1, Component=2
        int Order,
        string Name,                    // Zamiast w FieldValues
        decimal? Quantity,              // NOWE — direct property
        string? Unit,                   // NOWE
        decimal? UnitPriceNet,          // NOWE
        decimal? VatRate,               // NOWE
        decimal? UnitPriceGross,        // NOWE
        decimal? NetValue,              // Obliczona wartość netto
        decimal? GrossValue,            // Obliczona wartość brutto
        decimal? VatValue,              // Obliczona wartość VAT
        bool IsSelected,                // NOWE
        bool IsStageWork,               // NOWE
        List<CostEstimateAdditionalFieldValueWeb> AdditionalFieldValues, // NOWE
        List<CostEstimateItemWeb>? Options,      // Kolekcja opcji
        List<CostEstimateItemWeb>? Components,   // Kolekcja komponentów
        List<CostEstimateItemFileWeb>? Files,    // NOWE
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    /// <summary>
    /// Grupa kosztorysu
    /// </summary>
    public sealed record CostEstimateGroupWeb(
        Guid Id,
        Guid? ParentGroupId,
        int Level,
        int Order,
        string Name,                    // Zamiast w FieldValues
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        List<CostEstimateAdditionalFieldValueWeb> AdditionalFieldValues, // NOWE
        DateTime? LastCalculatedAt,
        List<CostEstimateGroupWeb> ChildGroups,
        List<CostEstimateItemWeb> Items,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
