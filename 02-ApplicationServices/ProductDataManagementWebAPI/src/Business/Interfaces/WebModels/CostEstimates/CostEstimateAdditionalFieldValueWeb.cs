namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Web model dla wartości pola dodatkowego kosztorysu
    /// </summary>
    public sealed record CostEstimateAdditionalFieldValueWeb(
        Guid Id,
        Guid AdditionalFieldId,
        string? StringValue,
        decimal? DecimalValue,
        bool? BoolValue,
        DateTime? DateTimeValue
    );
}
