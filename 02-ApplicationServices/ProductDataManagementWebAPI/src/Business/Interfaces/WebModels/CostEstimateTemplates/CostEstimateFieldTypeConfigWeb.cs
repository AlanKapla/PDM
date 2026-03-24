using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Konfiguracja typu pola w szablonie kosztorysu
    /// Zawiera metadane dotyczące typu pola: zakres, nazwę przyjazną, typ danych
    /// </summary>
    public record CostEstimateFieldTypeConfigWeb(
        int FieldType,
        int FieldScope,
        string NamePl,
        string ValueTypeName,
        bool IsNumeric,
        bool IsText,
        bool IsDate,
        bool IsBoolean,
        bool IsCollection,
        bool IsFile = false
    );
}
