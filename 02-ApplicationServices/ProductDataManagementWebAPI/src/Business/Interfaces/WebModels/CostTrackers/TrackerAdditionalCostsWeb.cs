namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record TrackerAdditionalCostsWeb
    {
        public decimal? TotalNet { get; init; }
        public decimal? TotalGross { get; init; }
        public required int CostsCount { get; init; }
        public required List<TrackedCostWeb> Costs { get; init; }
    }
}
