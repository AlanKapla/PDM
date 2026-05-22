using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;

namespace Business.Implementation.Services
{
    public sealed class CostTrackerTimelineService : ICostTrackerTimelineService
    {
        /// <summary>
        /// Priorytety agregacji — im wyższy, tym gorszy status.
        /// Kolejność: Delayed > CompletedLate > InProgress > NotStarted > Completed > NoSchedule.
        /// </summary>
        private static readonly Dictionary<TimelineStatus, int> StatusPriority = new()
        {
            [TimelineStatus.NoSchedule]    = 0,
            [TimelineStatus.NotConfigured] = 0,
            [TimelineStatus.Completed]     = 1,
            [TimelineStatus.NotStarted]    = 2,
            [TimelineStatus.InProgress]    = 3,
            [TimelineStatus.CompletedLate] = 4,
            [TimelineStatus.Delayed]       = 5,
        };

        public TimelineStatus ComputeItemStatus(DateTime? plannedStart, DateTime? plannedEnd, DateTime referenceDate)
        {
            if (!plannedStart.HasValue)
                return TimelineStatus.NoSchedule;

            if (referenceDate < plannedStart.Value)
                return TimelineStatus.NotStarted;

            if (!plannedEnd.HasValue || referenceDate <= plannedEnd.Value)
                return TimelineStatus.InProgress;

            return TimelineStatus.Delayed;
        }

        public TimelineStatus AggregateStatuses(IEnumerable<TimelineStatus> statuses)
        {
            List<TimelineStatus> list = statuses.ToList();

            if (list.Count == 0)
                return TimelineStatus.NoSchedule;

            List<TimelineStatus> significant = list
                .Where(s => s != TimelineStatus.NoSchedule && s != TimelineStatus.NotConfigured)
                .ToList();

            if (significant.Count == 0)
                return list.Any(s => s == TimelineStatus.NotConfigured)
                    ? TimelineStatus.NotConfigured
                    : TimelineStatus.NoSchedule;

            if (significant.All(s => s == TimelineStatus.Completed))
                return TimelineStatus.Completed;

            return significant.MaxBy(s => StatusPriority[s]);
        }

        public TimelineStatsWeb? BuildTimelineStats(IReadOnlyList<WorkItemLinkWeb> linkedItems, DateTime referenceDate)
        {
            List<WorkItemLinkWeb> scheduled = linkedItems.Where(i => i.HasLinkedSchedule).ToList();

            if (scheduled.Count == 0)
                return null;

            int completedCount     = scheduled.Count(i => i.Timeline?.OverallStatus == TimelineStatus.Completed);
            int completedLateCount = scheduled.Count(i => i.Timeline?.OverallStatus == TimelineStatus.CompletedLate);
            int inProgressCount    = scheduled.Count(i => i.Timeline?.OverallStatus == TimelineStatus.InProgress);
            int notStartedCount    = scheduled.Count(i => i.Timeline?.OverallStatus == TimelineStatus.NotStarted);
            int delayedCount       = scheduled.Count(i => i.Timeline?.OverallStatus == TimelineStatus.Delayed);
            int totalWorkCount     = scheduled.Count;

            DateTime? plannedStart = scheduled
                .Where(i => i.Timeline?.PlannedStart.HasValue == true)
                .Select(i => i.Timeline!.PlannedStart!.Value)
                .DefaultIfEmpty()
                .Min() is DateTime minDate && minDate != default ? minDate : null;

            DateTime? plannedEnd = scheduled
                .Where(i => i.Timeline?.PlannedEnd.HasValue == true)
                .Select(i => i.Timeline!.PlannedEnd!.Value)
                .DefaultIfEmpty()
                .Max() is DateTime maxDate && maxDate != default ? maxDate : null;

            double? totalPlannedDays = plannedStart.HasValue && plannedEnd.HasValue
                ? (plannedEnd.Value - plannedStart.Value).TotalDays
                : null;

            decimal? progressPercent = totalWorkCount > 0
                ? Math.Round((decimal)(completedCount + completedLateCount) / totalWorkCount * 100, 4)
                : null;

            double? delayDays = scheduled
                .Where(i => i.Timeline?.DelayDays.HasValue == true)
                .Select(i => i.Timeline!.DelayDays!.Value)
                .DefaultIfEmpty(0)
                .Max() is double max && max > 0 ? max : null;

            TimelineStatus overallStatus = AggregateStatuses(scheduled.Select(i => i.Timeline?.OverallStatus ?? TimelineStatus.NoSchedule));

            return new TimelineStatsWeb
            {
                PlannedStart       = plannedStart,
                PlannedEnd         = plannedEnd,
                TotalPlannedDays   = totalPlannedDays,
                TotalWorkCount     = totalWorkCount,
                CompletedCount     = completedCount,
                CompletedLateCount = completedLateCount,
                InProgressCount    = inProgressCount,
                NotStartedCount    = notStartedCount,
                DelayedCount       = delayedCount,
                ProgressPercent    = progressPercent,
                DelayDays          = delayDays,
                OverallStatus      = overallStatus,
                IsDelayed          = overallStatus is TimelineStatus.Delayed or TimelineStatus.CompletedLate,
                IsCompleted        = overallStatus is TimelineStatus.Completed or TimelineStatus.CompletedLate,
            };
        }

        public TimelineStatsWeb? AggregateTimelineStats(IEnumerable<TimelineStatsWeb?> childStats, DateTime referenceDate)
        {
            List<TimelineStatsWeb> stats = childStats.Where(s => s != null).Select(s => s!).ToList();

            if (stats.Count == 0)
                return null;

            int totalWorkCount     = stats.Sum(s => s.TotalWorkCount);
            int completedCount     = stats.Sum(s => s.CompletedCount);
            int completedLateCount = stats.Sum(s => s.CompletedLateCount);
            int inProgressCount    = stats.Sum(s => s.InProgressCount);
            int notStartedCount    = stats.Sum(s => s.NotStartedCount);
            int delayedCount       = stats.Sum(s => s.DelayedCount);

            DateTime? plannedStart = stats
                .Where(s => s.PlannedStart.HasValue)
                .Select(s => s.PlannedStart!.Value)
                .DefaultIfEmpty()
                .Min() is DateTime minDate && minDate != default ? minDate : null;

            DateTime? plannedEnd = stats
                .Where(s => s.PlannedEnd.HasValue)
                .Select(s => s.PlannedEnd!.Value)
                .DefaultIfEmpty()
                .Max() is DateTime maxDate && maxDate != default ? maxDate : null;

            double? totalPlannedDays = plannedStart.HasValue && plannedEnd.HasValue
                ? (plannedEnd.Value - plannedStart.Value).TotalDays
                : null;

            decimal? progressPercent = totalWorkCount > 0
                ? Math.Round((decimal)(completedCount + completedLateCount) / totalWorkCount * 100, 4)
                : null;

            double? delayDays = stats
                .Where(s => s.DelayDays.HasValue)
                .Select(s => s.DelayDays!.Value)
                .DefaultIfEmpty(0)
                .Max() is double max && max > 0 ? max : null;

            TimelineStatus overallStatus = AggregateStatuses(stats.Select(s => s.OverallStatus));

            return new TimelineStatsWeb
            {
                PlannedStart       = plannedStart,
                PlannedEnd         = plannedEnd,
                TotalPlannedDays   = totalPlannedDays,
                TotalWorkCount     = totalWorkCount,
                CompletedCount     = completedCount,
                CompletedLateCount = completedLateCount,
                InProgressCount    = inProgressCount,
                NotStartedCount    = notStartedCount,
                DelayedCount       = delayedCount,
                ProgressPercent    = progressPercent,
                DelayDays          = delayDays,
                OverallStatus      = overallStatus,
                IsDelayed          = overallStatus is TimelineStatus.Delayed or TimelineStatus.CompletedLate,
                IsCompleted        = overallStatus is TimelineStatus.Completed or TimelineStatus.CompletedLate,
            };
        }
    }
}
