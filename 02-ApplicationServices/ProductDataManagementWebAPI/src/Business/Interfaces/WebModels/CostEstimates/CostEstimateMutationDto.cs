using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// DTO dla tworzenia/edycji wartości pola (wspólny dla grup i pozycji)
    /// Używa pojedynczego FieldDefinitionId wskazującego na definicję pola w szablonie
    /// Wartość zapisywana w odpowiednim polu typowanym w zależności od FieldType
    /// </summary>
    public record CostEstimateFieldValueDto(
        Guid FieldDefinitionId,
        string? StringValue,
        decimal? DecimalValue,
        bool? BoolValue,
        DateTime? DateTimeValue
    );

    /// <summary>
    /// DTO dla tworzenia/edycji pozycji kosztorysu
    /// Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
    /// Może zawierać kolekcję Components - wtedy NIE MOŻE mieć FieldValues!
    /// WAŻNE: Options i Components mogą mieć tylko 1 poziom zagnieżdżenia (child nie może mieć childa)
    /// </summary>
    public record CostEstimateItemDto(
        Guid? Id,  // null dla nowych pozycji
        Guid? ParentItemId,  // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
        ItemRelationType RelationType,  // None/Option/Component
        int Order,
        List<CostEstimateFieldValueDto> FieldValues,
        List<CostEstimateItemDto>? Options,  // Kolekcja opcji - max 1 poziom zagnieżdżenia! Jeśli ParentItemId != null → NIE MOŻE mieć Options
        List<CostEstimateItemDto>? Components  // Kolekcja komponentów - max 1 poziom zagnieżdżenia! Jeśli ParentItemId != null → NIE MOŻE mieć Components
    );

    /// <summary>
    /// DTO dla tworzenia/edycji grupy kosztorysu
    /// </summary>
    public record CostEstimateGroupDto(
        Guid? Id,  // null dla nowych grup
        Guid? ParentGroupId,
        int Level,
        int Order,
        List<CostEstimateFieldValueDto> FieldValues,
        List<CostEstimateItemDto> Items,
        List<CostEstimateGroupDto> ChildGroups
    );
}
