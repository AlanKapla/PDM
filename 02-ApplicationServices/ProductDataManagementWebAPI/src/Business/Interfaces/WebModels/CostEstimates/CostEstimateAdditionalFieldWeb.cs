namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Web model dla pola dodatkowego kosztorysu
    /// </summary>
    public sealed record CostEstimateAdditionalFieldWeb(
        Guid Id,
        Guid CostEstimateId,
        string Name,
        int FieldType,
        int Order,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
