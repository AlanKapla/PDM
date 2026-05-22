using Business.Implementation.Services;
using Business.Interfaces.WebModels.CostTrackers;
using FluentAssertions;

namespace Business.Tests.Services;

public class CostTrackerTimelineServiceTests
{
    private readonly CostTrackerTimelineService _sut = new CostTrackerTimelineService();

    private static readonly DateTime Reference = new DateTime(2024, 6, 15);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static WorkItemLinkWeb BuildLinkedItem(
        bool hasLinkedSchedule = true,
        TimelineStatsWeb? timeline = null)
        => new WorkItemLinkWeb
        {
            // StatusedNodeWeb
            FinancialStatus = FinancialStatus.InProgress,
            TimelineStatus = TimelineStatus.NoSchedule,
            // TrackerNodeWeb
            IsBudgetExceeded = false,
            CostCount = 0,
            // TrackerNodeWithTimelineWeb
            HasLinkedSchedule = hasLinkedSchedule,
            Timeline = timeline,
            // WorkItemLinkWeb
            DisplayName = "Item",
            Order = 1,
            WorkItemType = WorkItemType.EstimateItem,
            Costs = new List<TrackedCostWeb>()
        };

    private static TimelineStatsWeb BuildStats(
        TimelineStatus status,
        DateTime? plannedStart = null,
        DateTime? plannedEnd = null,
        int totalWorkCount = 1,
        int completedCount = 0,
        int completedLateCount = 0,
        int inProgressCount = 0,
        int notStartedCount = 0,
        int delayedCount = 0,
        double? delayDays = null)
        => new TimelineStatsWeb
        {
            PlannedStart = plannedStart,
            PlannedEnd = plannedEnd,
            TotalWorkCount = totalWorkCount,
            CompletedCount = completedCount,
            CompletedLateCount = completedLateCount,
            InProgressCount = inProgressCount,
            NotStartedCount = notStartedCount,
            DelayedCount = delayedCount,
            OverallStatus = status,
            IsDelayed = status is TimelineStatus.Delayed or TimelineStatus.CompletedLate,
            IsCompleted = status is TimelineStatus.Completed or TimelineStatus.CompletedLate,
            DelayDays = delayDays
        };

    // ─── ComputeItemStatus ────────────────────────────────────────────────────

    [Fact]
    public void ComputeItemStatus_NullPlannedStart_ReturnsNoSchedule()
    {
        TimelineStatus result = _sut.ComputeItemStatus(null, null, Reference);

        result.Should().Be(TimelineStatus.NoSchedule);
    }

    [Fact]
    public void ComputeItemStatus_BeforePlannedStart_ReturnsNotStarted()
    {
        TimelineStatus result = _sut.ComputeItemStatus(
            plannedStart: Reference.AddDays(1),
            plannedEnd: Reference.AddDays(10),
            referenceDate: Reference);

        result.Should().Be(TimelineStatus.NotStarted);
    }

    [Fact]
    public void ComputeItemStatus_BetweenStartAndEnd_ReturnsInProgress()
    {
        TimelineStatus result = _sut.ComputeItemStatus(
            plannedStart: Reference.AddDays(-5),
            plannedEnd: Reference.AddDays(5),
            referenceDate: Reference);

        result.Should().Be(TimelineStatus.InProgress);
    }

    [Fact]
    public void ComputeItemStatus_AtPlannedEnd_ReturnsInProgress()
    {
        TimelineStatus result = _sut.ComputeItemStatus(
            plannedStart: Reference.AddDays(-10),
            plannedEnd: Reference,
            referenceDate: Reference);

        result.Should().Be(TimelineStatus.InProgress);
    }

    [Fact]
    public void ComputeItemStatus_AfterPlannedEnd_ReturnsDelayed()
    {
        TimelineStatus result = _sut.ComputeItemStatus(
            plannedStart: Reference.AddDays(-10),
            plannedEnd: Reference.AddDays(-1),
            referenceDate: Reference);

        result.Should().Be(TimelineStatus.Delayed);
    }

    [Fact]
    public void ComputeItemStatus_NullPlannedEnd_ReturnsInProgress()
    {
        // No end date → always in progress once started
        TimelineStatus result = _sut.ComputeItemStatus(
            plannedStart: Reference.AddDays(-5),
            plannedEnd: null,
            referenceDate: Reference);

        result.Should().Be(TimelineStatus.InProgress);
    }

    // ─── AggregateStatuses ────────────────────────────────────────────────────

    [Fact]
    public void AggregateStatuses_EmptyList_ReturnsNoSchedule()
    {
        TimelineStatus result = _sut.AggregateStatuses(Enumerable.Empty<TimelineStatus>());

        result.Should().Be(TimelineStatus.NoSchedule);
    }

    [Fact]
    public void AggregateStatuses_AllNoSchedule_ReturnsNoSchedule()
    {
        TimelineStatus result = _sut.AggregateStatuses(new[]
        {
            TimelineStatus.NoSchedule,
            TimelineStatus.NoSchedule
        });

        result.Should().Be(TimelineStatus.NoSchedule);
    }

    [Fact]
    public void AggregateStatuses_AllCompleted_ReturnsCompleted()
    {
        TimelineStatus result = _sut.AggregateStatuses(new[]
        {
            TimelineStatus.Completed,
            TimelineStatus.Completed
        });

        result.Should().Be(TimelineStatus.Completed);
    }

    [Fact]
    public void AggregateStatuses_DelayedAndInProgress_ReturnsDelayed()
    {
        TimelineStatus result = _sut.AggregateStatuses(new[]
        {
            TimelineStatus.InProgress,
            TimelineStatus.Delayed
        });

        result.Should().Be(TimelineStatus.Delayed);
    }

    [Fact]
    public void AggregateStatuses_NotConfiguredOnly_ReturnsNotConfigured()
    {
        TimelineStatus result = _sut.AggregateStatuses(new[]
        {
            TimelineStatus.NotConfigured
        });

        result.Should().Be(TimelineStatus.NotConfigured);
    }

    [Fact]
    public void AggregateStatuses_MixedWithNoSchedule_ReturnsWorstSignificant()
    {
        TimelineStatus result = _sut.AggregateStatuses(new[]
        {
            TimelineStatus.NoSchedule,
            TimelineStatus.InProgress,
            TimelineStatus.NotStarted
        });

        result.Should().Be(TimelineStatus.InProgress);
    }

    // ─── BuildTimelineStats ───────────────────────────────────────────────────

    [Fact]
    public void BuildTimelineStats_NoScheduledItems_ReturnsNull()
    {
        List<WorkItemLinkWeb> items = new List<WorkItemLinkWeb>
        {
            BuildLinkedItem(hasLinkedSchedule: false),
            BuildLinkedItem(hasLinkedSchedule: false)
        };

        TimelineStatsWeb? result = _sut.BuildTimelineStats(items, Reference);

        result.Should().BeNull();
    }

    [Fact]
    public void BuildTimelineStats_EmptyList_ReturnsNull()
    {
        TimelineStatsWeb? result = _sut.BuildTimelineStats(new List<WorkItemLinkWeb>(), Reference);

        result.Should().BeNull();
    }

    [Fact]
    public void BuildTimelineStats_TwoItems_CountsCorrectly()
    {
        TimelineStatsWeb completedStats = BuildStats(TimelineStatus.Completed,
            plannedStart: new DateTime(2024, 1, 1),
            plannedEnd: new DateTime(2024, 3, 1),
            completedCount: 1);

        TimelineStatsWeb delayedStats = BuildStats(TimelineStatus.Delayed,
            plannedStart: new DateTime(2024, 2, 1),
            plannedEnd: new DateTime(2024, 5, 1),
            delayedCount: 1,
            delayDays: 15);

        List<WorkItemLinkWeb> items = new List<WorkItemLinkWeb>
        {
            BuildLinkedItem(timeline: completedStats),
            BuildLinkedItem(timeline: delayedStats)
        };

        TimelineStatsWeb? result = _sut.BuildTimelineStats(items, Reference);

        result.Should().NotBeNull();
        result!.TotalWorkCount.Should().Be(2);
        result.CompletedCount.Should().Be(1);
        result.DelayedCount.Should().Be(1);
        result.OverallStatus.Should().Be(TimelineStatus.Delayed);
        result.IsDelayed.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void BuildTimelineStats_ProgressPercent_CalculatedFromCompleted()
    {
        TimelineStatsWeb statsA = BuildStats(TimelineStatus.Completed,
            completedCount: 1, totalWorkCount: 1);
        TimelineStatsWeb statsB = BuildStats(TimelineStatus.InProgress,
            inProgressCount: 1, totalWorkCount: 1);
        TimelineStatsWeb statsC = BuildStats(TimelineStatus.InProgress,
            inProgressCount: 1, totalWorkCount: 1);

        List<WorkItemLinkWeb> items = new List<WorkItemLinkWeb>
        {
            BuildLinkedItem(timeline: statsA),
            BuildLinkedItem(timeline: statsB),
            BuildLinkedItem(timeline: statsC)
        };

        TimelineStatsWeb? result = _sut.BuildTimelineStats(items, Reference);

        // 1 completed / 3 total * 100 ≈ 33.3333
        result!.ProgressPercent.Should().Be(Math.Round(1m / 3m * 100m, 4));
    }

    [Fact]
    public void BuildTimelineStats_PlannedDates_UsesMinStartMaxEnd()
    {
        DateTime start1 = new DateTime(2024, 1, 1);
        DateTime start2 = new DateTime(2024, 3, 1);
        DateTime end1 = new DateTime(2024, 6, 1);
        DateTime end2 = new DateTime(2024, 9, 1);

        List<WorkItemLinkWeb> items = new List<WorkItemLinkWeb>
        {
            BuildLinkedItem(timeline: BuildStats(TimelineStatus.InProgress,
                plannedStart: start1, plannedEnd: end1)),
            BuildLinkedItem(timeline: BuildStats(TimelineStatus.InProgress,
                plannedStart: start2, plannedEnd: end2))
        };

        TimelineStatsWeb? result = _sut.BuildTimelineStats(items, Reference);

        result!.PlannedStart.Should().Be(start1);
        result.PlannedEnd.Should().Be(end2);
    }

    // ─── AggregateTimelineStats ───────────────────────────────────────────────

    [Fact]
    public void AggregateTimelineStats_AllNull_ReturnsNull()
    {
        TimelineStatsWeb? result = _sut.AggregateTimelineStats(
            new TimelineStatsWeb?[] { null, null }, Reference);

        result.Should().BeNull();
    }

    [Fact]
    public void AggregateTimelineStats_TwoStats_SumsCountsAndTakesWorstStatus()
    {
        TimelineStatsWeb stats1 = BuildStats(TimelineStatus.Completed,
            totalWorkCount: 2, completedCount: 2,
            plannedStart: new DateTime(2024, 1, 1), plannedEnd: new DateTime(2024, 4, 1));

        TimelineStatsWeb stats2 = BuildStats(TimelineStatus.Delayed,
            totalWorkCount: 1, delayedCount: 1,
            plannedStart: new DateTime(2024, 2, 1), plannedEnd: new DateTime(2024, 5, 1),
            delayDays: 10);

        TimelineStatsWeb? result = _sut.AggregateTimelineStats(new[] { stats1, stats2 }, Reference);

        result.Should().NotBeNull();
        result!.TotalWorkCount.Should().Be(3);
        result.CompletedCount.Should().Be(2);
        result.DelayedCount.Should().Be(1);
        result.OverallStatus.Should().Be(TimelineStatus.Delayed);
        result.DelayDays.Should().Be(10);
    }
}
