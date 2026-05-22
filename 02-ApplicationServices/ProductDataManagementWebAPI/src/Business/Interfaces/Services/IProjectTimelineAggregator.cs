using Business.Interfaces.WebModels.CostTrackers;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Oblicza zagregowane TimelineSummary dla projektu na podstawie kosztorysów i harmonogramów.
    /// </summary>
    public interface IProjectTimelineAggregator
    {
        ProjectTimelineSummaryWeb Build(
            IReadOnlyCollection<CostEstimateSummaryWeb> estimateSummaries,
            IReadOnlyCollection<ScheduleSummaryWeb> scheduleSummaries);
    }
}
