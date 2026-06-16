namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Web model dla pliku dołączonego do pozycji kosztorysu.
    /// Zastępuje stary CostEstimateFieldFileWeb.
    /// </summary>
    public sealed record CostEstimateItemFileWeb(
        Guid Id,
        Guid ItemId,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        int Order,
        string? SasUriPreview,
        string? SasUriDownload,
        DateTime CreatedAt
    );
}
