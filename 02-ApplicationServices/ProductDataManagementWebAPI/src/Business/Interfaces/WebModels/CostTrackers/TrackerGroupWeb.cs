namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record TrackerGroupWeb : TrackerNodeWithTimelineWeb
    {
        public required Guid GroupId { get; init; }
        public required string GroupName { get; init; }
        public required int Order { get; init; }
        public required int TotalItemsCount { get; init; }
        public required int ItemsWithCostsCount { get; init; }
        public required int ItemsWithoutCostsCount { get; init; }
        public required int ItemsOverBudgetCount { get; init; }
        public required int ItemsNearLimitCount { get; init; }
        public required List<WorkItemLinkWeb> Items { get; init; }
        public required List<TrackerGroupWeb> ChildGroups { get; init; }

        /// <summary>Pokrycie budżetu przez koszty: (CostsNet / BudgetNet) * 100. Null gdy brak budżetu.</summary>
        public decimal? BudgetCoveredPercent { get; init; }

        /// <summary>Planowana data rozpoczęcia grupy (z Timeline.PlannedStart). Null gdy brak harmonogramu.</summary>
        public DateOnly? TimelinePlannedStart { get; init; }

        /// <summary>Planowana data zakończenia grupy (z Timeline.PlannedEnd). Null gdy brak harmonogramu.</summary>
        public DateOnly? TimelinePlannedEnd { get; init; }

        /// <summary>Łączna liczba planowanych dni grupy. Null gdy brak dat.</summary>
        public int? TimelineTotalDays { get; init; }
    }
}
