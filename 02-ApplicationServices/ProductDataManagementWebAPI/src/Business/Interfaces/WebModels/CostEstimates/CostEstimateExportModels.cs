namespace Business.Interfaces.WebModels.CostEstimates
{
    public enum CostEstimateExportFormat
    {
        Pdf = 0,
        Xlsx = 1
    }

    public enum CostEstimateExportRowType
    {
        Group = 0,
        Item = 1,
        Option = 2,
        Component = 3
    }

    public sealed record CostEstimateExportFile(
        byte[] Content,
        string ContentType,
        string FileName);

    public sealed record CostEstimateExportMeta(
        string Name,
        string? CurrencyCode,
        string? CurrencySymbol,
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        DateTime ExportedAtUtc);

    public sealed record CostEstimateExportRow(
        CostEstimateExportRowType RowType,
        int Level,
        string Name,
        decimal? Quantity,
        string? Unit,
        decimal? UnitPriceNet,
        decimal? VatRate,
        decimal? UnitPriceGross,
        decimal? NetValue,
        decimal? VatValue,
        decimal? GrossValue,
        bool? IsSelected,
        IReadOnlyDictionary<string, string?> AdditionalValues);
}
