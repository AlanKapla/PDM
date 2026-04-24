namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Węzeł finansowy z opcjonalnym powiązaniem z harmonogramem.
    /// Timeline = null gdy węzeł nie ma powiązanego harmonogramu.
    /// TimelineStatus pochodzi z Timeline.OverallStatus (lub NoSchedule gdy Timeline = null).
    /// </summary>
    public abstract record TrackerNodeWithTimelineWeb : TrackerNodeWeb
    {
        /// <summary>Statystyki czasowe. Null gdy brak powiązanego harmonogramu.</summary>
        public TimelineStatsWeb? Timeline { get; init; }

        /// <summary>True gdy węzeł ma powiązany harmonogram.</summary>
        public required bool HasLinkedSchedule { get; init; }
    }
}
