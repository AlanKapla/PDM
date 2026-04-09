namespace Business.Interfaces.WebModels.CostTrackers
{
    public record CostTrackerDetailsWeb
    {
        public required Guid Id { get; init; }
        public required Guid ProjectId { get; init; }
        public required CostTrackerSummaryWeb Summary { get; init; }
        public required List<CostEstimateSummaryWeb> CostEstimateSummaries { get; init; }
        public required ProjectAdditionalCostsWeb ProjectAdditionalCosts { get; init; }
    }
}
