namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Cache model for SAS URIs per file.
    /// </summary>
    public sealed class CostEstimateFieldFileSasInfo
    {
        public required string PreviewUri { get; init; }
        public required string DownloadUri { get; init; }
    }
}
