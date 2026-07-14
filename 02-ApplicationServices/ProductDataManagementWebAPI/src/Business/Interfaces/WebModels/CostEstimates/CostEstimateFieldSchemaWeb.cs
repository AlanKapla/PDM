namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Web model wpisu schematu kolumn kosztorysu.
    /// </summary>
    public sealed record CostEstimateFieldSchemaWeb(
        Guid Id,
        Guid CostEstimateId,
        string FieldName,
        string FieldKey,
        int FieldType,
        bool IsBasicField,
        bool IsAdditionalField,
        int Order,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
