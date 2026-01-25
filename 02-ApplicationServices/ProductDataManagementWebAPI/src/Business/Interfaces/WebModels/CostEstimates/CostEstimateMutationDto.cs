using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// DTO dla tworzenia/edycji wartości pola grupy
    /// </summary>
    public record CostEstimateGroupFieldValueDto(
        Guid FieldDefinitionId,
        string? Value
    );

    /// <summary>
    /// DTO dla tworzenia/edycji wartości pola pozycji
    /// Używa pojedynczego FieldDefinitionId wskazującego na CostEstimateTemplateFieldDefinitionBase
    /// </summary>
    public record CostEstimateFieldValueDto(
        Guid FieldDefinitionId,
        string? Value
    );

    /// <summary>
    /// DTO dla tworzenia/edycji pozycji kosztorysu
    /// Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
    /// </summary>
    public record CostEstimateItemDto(
        Guid? Id,  // null dla nowych pozycji
        Guid? ParentItemId,  // ID pozycji nadrzędnej (jeśli to opcja)
        int Order,
        List<CostEstimateFieldValueDto> FieldValues,
        List<CostEstimateItemDto>? Options  // Kolekcja opcji - max 1 poziom zagnieżdżenia!
    );

    /// <summary>
    /// DTO dla tworzenia/edycji grupy kosztorysu
    /// </summary>
    public record CostEstimateGroupDto(
        Guid? Id,  // null dla nowych grup
        Guid? ParentGroupId,
        int Level,
        int Order,
        List<CostEstimateGroupFieldValueDto> FieldValues,
        List<CostEstimateItemDto> Items,
        List<CostEstimateGroupDto> ChildGroups
    );
}
