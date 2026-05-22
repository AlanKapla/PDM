using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;

namespace Business.Implementation.Services
{
    public sealed class ProjectTimelineAggregator : IProjectTimelineAggregator
    {
        public ProjectTimelineSummaryWeb Build(
            IReadOnlyCollection<CostEstimateSummaryWeb> estimateSummaries,
            IReadOnlyCollection<ScheduleSummaryWeb> scheduleSummaries)
        {
            List<TimelineStatsWeb> stats = scheduleSummaries
                .Where(s => s.Timeline is not null)
                .Select(s => s.Timeline!)
                .ToList();

            int totalWorkCount     = stats.Sum(s => s.TotalWorkCount);
            int completedCount     = stats.Sum(s => s.CompletedCount);
            int completedLateCount = stats.Sum(s => s.CompletedLateCount);
            int inProgressCount    = stats.Sum(s => s.InProgressCount);
            int notStartedCount    = stats.Sum(s => s.NotStartedCount);
            int delayedCount       = stats.Sum(s => s.DelayedCount);

            TimelineStatus overallStatus = ResolveOverallStatus(scheduleSummaries, stats);

            decimal? progressPercent = totalWorkCount > 0
                ? Math.Round((completedCount + completedLateCount) / (decimal)totalWorkCount * 100, 2)
                : null;

            double? delayDays = stats.Any(s => s.DelayDays.HasValue)
                ? stats.Max(s => s.DelayDays ?? 0)
                : null;

            DateTime? earliestStart = stats.Any(s => s.PlannedStart.HasValue)
                ? stats.Where(s => s.PlannedStart.HasValue).Min(s => s.PlannedStart!.Value)
                : null;

            DateTime? latestEnd = stats.Any(s => s.PlannedEnd.HasValue)
                ? stats.Where(s => s.PlannedEnd.HasValue).Max(s => s.PlannedEnd!.Value)
                : null;

            double? totalPlannedDays = earliestStart.HasValue && latestEnd.HasValue
                ? (latestEnd.Value - earliestStart.Value).TotalDays
                : null;

            return new ProjectTimelineSummaryWeb
            {
                EarliestStart           = earliestStart,
                LatestEnd               = latestEnd,
                TotalPlannedDays        = totalPlannedDays,
                TotalWorkCount          = totalWorkCount,
                CompletedCount          = completedCount,
                CompletedLateCount      = completedLateCount,
                InProgressCount         = inProgressCount,
                NotStartedCount         = notStartedCount,
                DelayedCount            = delayedCount,
                ProgressPercent         = progressPercent,
                DelayDays               = delayDays,
                OverallStatus           = overallStatus,
                IsDelayed               = overallStatus is TimelineStatus.Delayed or TimelineStatus.CompletedLate,
                IsCompleted             = overallStatus is TimelineStatus.Completed or TimelineStatus.CompletedLate,
                WorkSchedulesCount      = scheduleSummaries.Count,
                ActiveSchedulesCount    = scheduleSummaries.Count(s => s.TimelineStatus == TimelineStatus.InProgress),
                CompletedSchedulesCount = scheduleSummaries.Count(s => s.TimelineStatus is TimelineStatus.Completed or TimelineStatus.CompletedLate)
            };
        }

        private static TimelineStatus ResolveOverallStatus(
            IReadOnlyCollection<ScheduleSummaryWeb> scheduleSummaries,
            List<TimelineStatsWeb> stats)
        {
            if (scheduleSummaries.Count == 0)
            {
                return TimelineStatus.NoSchedule;
            }

            if (scheduleSummaries.Sum(s => s.TotalWorkItemsCount) == 0)
            {
                return TimelineStatus.NotConfigured;
            }

            if (stats.Count == 0)
            {
                return scheduleSummaries.Any(s => s.TimelineStatus == TimelineStatus.NotConfigured)
                    ? TimelineStatus.NotConfigured
                    : TimelineStatus.NoSchedule;
            }

            if (stats.All(s => s.OverallStatus is TimelineStatus.NotConfigured or TimelineStatus.NoSchedule))
            {
                return stats.Any(s => s.OverallStatus == TimelineStatus.NotConfigured)
                    ? TimelineStatus.NotConfigured
                    : TimelineStatus.NoSchedule;
            }

            return stats.Select(s => s.OverallStatus).MaxBy(GetSeverity);
        }

        private static int GetSeverity(TimelineStatus status) => status switch
        {
            TimelineStatus.Delayed       => 5,
            TimelineStatus.CompletedLate => 4,
            TimelineStatus.InProgress    => 3,
            TimelineStatus.NotStarted    => 2,
            TimelineStatus.Completed     => 1,
            _                            => 0
        };
    }
}
