namespace Business.Interfaces.WebModels.CostTrackers
{
    public record TrackerGroupWeb : TrackerNodeWeb
    {
        public required Guid GroupId { get; init; }
        public required string GroupName { get; init; }
        public required int Order { get; init; }
        public required int TotalItemsCount { get; init; }
        public required int ItemsWithCostsCount { get; init; }
        public required List<TrackerItemWeb> Items { get; init; }
        public required List<TrackerGroupWeb> ChildGroups { get; init; }
    }
}
