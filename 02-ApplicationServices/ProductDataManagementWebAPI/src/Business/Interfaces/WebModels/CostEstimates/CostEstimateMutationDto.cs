namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// DTO dla tworzenia/edycji wartości pola dodatkowego (AdditionalField)
    /// </summary>
    public sealed record CostEstimateAdditionalFieldValueDto(
        Guid? Id,
        Guid AdditionalFieldId,
        string? StringValue,
        decimal? DecimalValue,
        bool? BoolValue,
        DateTime? DateTimeValue
    );

    /// <summary>
    /// DTO dla tworzenia/edycji pozycji kosztorysu
    /// Może zawierać kolekcję Options (warianty)
    /// Może zawierać kolekcję Components (składniki pozycji)
    /// WAŻNE: Options i Components mogą mieć tylko 1 poziom zagnieżdżenia (child nie może mieć childa)
    /// </summary>
    public sealed record CostEstimateItemDto(
        Guid? Id,  // null dla nowych pozycji
        Guid? ParentItemId,  // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
        int RelationType,  // ItemRelationType jako int: None=0, Option=1, Component=2
        int Order,
        string? Name,
        decimal? Quantity,
        string? Unit,
        decimal? UnitPriceNet,
        decimal? VatRate,
        List<CostEstimateAdditionalFieldValueDto> AdditionalFieldValues, // NOWE
        List<CostEstimateItemDto>? Options,
        List<CostEstimateItemDto>? Components
    );

    /// <summary>
    /// DTO dla tworzenia/edycji grupy kosztorysu
    /// </summary>
    public sealed record CostEstimateGroupDto(
        Guid? Id,  // null dla nowych grup
        Guid? ParentGroupId,
        int Level,
        int Order,
        string? Name,
        List<CostEstimateAdditionalFieldValueDto> AdditionalFieldValues,
        List<CostEstimateItemDto> Items,
        List<CostEstimateGroupDto> ChildGroups
    );
}
