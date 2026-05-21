using Business.Implementation.Services;
using Business.Interfaces.WebModels.CostTrackers;
using FluentAssertions;

namespace Business.Tests.Services;

public class CostTrackerFinancialServiceTests
{
    private readonly CostTrackerFinancialService _sut = new CostTrackerFinancialService();

    private static CostEstimateSummaryWeb BuildEstimate(
        decimal? budgetNet = null,
        decimal? budgetGross = null,
        decimal? costsNet = null,
        decimal? costsGross = null,
        int costCount = 0,
        int totalItemsCount = 0,
        int itemsWithCostsCount = 0)
        => new CostEstimateSummaryWeb
        {
            // StatusedNodeWeb
            FinancialStatus = FinancialStatus.InProgress,
            TimelineStatus = TimelineStatus.NoSchedule,
            // TrackerNodeWeb
            BudgetNet = budgetNet,
            BudgetGross = budgetGross,
            CostsNet = costsNet,
            CostsGross = costsGross,
            IsBudgetExceeded = false,
            CostCount = costCount,
            // TrackerNodeWithTimelineWeb
            HasLinkedSchedule = false,
            // CostEstimateSummaryWeb
            CostEstimateId = Guid.NewGuid(),
            CostEstimateName = "Test",
            TotalItemsCount = totalItemsCount,
            ItemsWithCostsCount = itemsWithCostsCount,
            ItemsWithoutCostsCount = totalItemsCount - itemsWithCostsCount,
            ItemsOverBudgetCount = 0,
            ItemsNearLimitCount = 0,
            Groups = new List<TrackerGroupWeb>()
        };

    private static ProjectAdditionalCostsWeb BuildAdditionalCosts(
        decimal? totalNet = null, decimal? totalGross = null, int costsCount = 0)
        => new ProjectAdditionalCostsWeb
        {
            TotalNet = totalNet,
            TotalGross = totalGross,
            CostsCount = costsCount,
            Costs = new List<TrackedCostWeb>()
        };

    // ─── Calculate ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_BothNetAndGross_ReturnsBothRounded()
    {
        (decimal? net, decimal? gross) = _sut.Calculate(100.1234m, 123.5678m);

        net.Should().Be(100.12m);
        gross.Should().Be(123.57m);
    }

    [Fact]
    public void Calculate_OnlyNet_ReturnsNetNullGross()
    {
        (decimal? net, decimal? gross) = _sut.Calculate(100m, null);

        net.Should().Be(100m);
        gross.Should().BeNull();
    }

    [Fact]
    public void Calculate_OnlyGross_ReturnsNullNetGross()
    {
        (decimal? net, decimal? gross) = _sut.Calculate(null, 123m);

        net.Should().BeNull();
        gross.Should().Be(123m);
    }

    [Fact]
    public void Calculate_BothNull_ReturnsBothNull()
    {
        (decimal? net, decimal? gross) = _sut.Calculate(null, null);

        net.Should().BeNull();
        gross.Should().BeNull();
    }

    // ─── ComputeItemStatus ────────────────────────────────────────────────────

    [Fact]
    public void ComputeItemStatus_ZeroCostsCount_ReturnsNoCosts()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: 1000m, costsNet: 500m, costsCount: 0);

        status.Should().Be(FinancialStatus.NoCosts);
    }

    [Fact]
    public void ComputeItemStatus_NullBudget_ReturnNoBudget()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: null, costsNet: 500m, costsCount: 3);

        status.Should().Be(FinancialStatus.NoBudget);
    }

    [Fact]
    public void ComputeItemStatus_ZeroBudget_ReturnNoBudget()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: 0m, costsNet: 500m, costsCount: 3);

        status.Should().Be(FinancialStatus.NoBudget);
    }

    [Fact]
    public void ComputeItemStatus_NullCosts_ReturnsInProgress()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: 1000m, costsNet: null, costsCount: 1);

        status.Should().Be(FinancialStatus.InProgress);
    }

    [Fact]
    public void ComputeItemStatus_CostsExceedBudget_ReturnsOverBudget()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: 1000m, costsNet: 1001m, costsCount: 1);

        status.Should().Be(FinancialStatus.OverBudget);
    }

    [Theory]
    [InlineData(800, 1000)]  // exactly 80%
    [InlineData(900, 1000)]  // 90%
    [InlineData(1000, 1000)] // 100%
    public void ComputeItemStatus_CostsAtOrAboveThreshold_ReturnsNearLimit(decimal costsNet, decimal budgetNet)
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: budgetNet, costsNet: costsNet, costsCount: 1);

        status.Should().Be(FinancialStatus.NearLimit);
    }

    [Fact]
    public void ComputeItemStatus_CostsBelowThreshold_ReturnsInProgress()
    {
        FinancialStatus status = _sut.ComputeItemStatus(budgetNet: 1000m, costsNet: 799m, costsCount: 1);

        status.Should().Be(FinancialStatus.InProgress);
    }

    // ─── ComputeFinancialStatus ───────────────────────────────────────────────

    [Fact]
    public void ComputeFinancialStatus_NullBudget_ReturnsNoBudget()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: null, costsNet: 500m);

        status.Should().Be(FinancialStatus.NoBudget);
    }

    [Fact]
    public void ComputeFinancialStatus_ZeroBudget_ReturnsNoBudget()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: 0m, costsNet: 100m);

        status.Should().Be(FinancialStatus.NoBudget);
    }

    [Fact]
    public void ComputeFinancialStatus_NullCosts_ReturnsNoCosts()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: 1000m, costsNet: null);

        status.Should().Be(FinancialStatus.NoCosts);
    }

    [Fact]
    public void ComputeFinancialStatus_CostsOverBudget_ReturnsOverBudget()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: 1000m, costsNet: 1001m);

        status.Should().Be(FinancialStatus.OverBudget);
    }

    [Fact]
    public void ComputeFinancialStatus_CostsAtNearLimitThreshold_ReturnsNearLimit()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: 1000m, costsNet: 800m);

        status.Should().Be(FinancialStatus.NearLimit);
    }

    [Fact]
    public void ComputeFinancialStatus_CostsBelowThreshold_ReturnsInProgress()
    {
        FinancialStatus status = _sut.ComputeFinancialStatus(budgetNet: 1000m, costsNet: 500m);

        status.Should().Be(FinancialStatus.InProgress);
    }

    // ─── ComputeProjectSummary ────────────────────────────────────────────────

    [Fact]
    public void ComputeProjectSummary_NoEstimates_ReturnsNullTotals()
    {
        ProjectAdditionalCostsWeb additionalCosts = BuildAdditionalCosts();

        CostTrackerSummaryWeb result = _sut.ComputeProjectSummary(
            new List<CostEstimateSummaryWeb>(), additionalCosts, null, null);

        result.TotalCostsNet.Should().BeNull();
        result.TotalBudgetNet.Should().BeNull();
        result.IsBudgetExceeded.Should().BeFalse();
    }

    [Fact]
    public void ComputeProjectSummary_SingleEstimateWithCosts_AggregatesCorrectly()
    {
        CostEstimateSummaryWeb estimate = BuildEstimate(
            budgetNet: 1000m, budgetGross: 1230m,
            costsNet: 600m, costsGross: 738m,
            costCount: 2, totalItemsCount: 5, itemsWithCostsCount: 3);

        ProjectAdditionalCostsWeb additionalCosts = BuildAdditionalCosts(100m, 123m, 1);

        CostTrackerSummaryWeb result = _sut.ComputeProjectSummary(
            new List<CostEstimateSummaryWeb> { estimate },
            additionalCosts,
            budgetNet: 200m,
            budgetGross: 246m);

        result.TotalCostsNet.Should().Be(700m);      // 600 + 100
        result.TotalBudgetNet.Should().Be(1200m);    // 1000 + 200
        result.TotalDeviationNet.Should().Be(-500m); // 700 - 1200
        result.IsBudgetExceeded.Should().BeFalse();
        result.CostCount.Should().Be(3);             // 2 + 1
    }

    [Fact]
    public void ComputeProjectSummary_CostsExceedBudget_IsBudgetExceededTrue()
    {
        CostEstimateSummaryWeb estimate = BuildEstimate(
            budgetNet: 100m, costsNet: 200m,
            costCount: 1, totalItemsCount: 1, itemsWithCostsCount: 1);

        ProjectAdditionalCostsWeb additionalCosts = BuildAdditionalCosts();

        CostTrackerSummaryWeb result = _sut.ComputeProjectSummary(
            new List<CostEstimateSummaryWeb> { estimate },
            additionalCosts, null, null);

        result.IsBudgetExceeded.Should().BeTrue();
        result.TotalDeviationPercent.Should().Be(100m); // (200-100)/100*100
    }

    [Fact]
    public void ComputeProjectSummary_CoveredPercent_CalculatedCorrectly()
    {
        CostEstimateSummaryWeb estimate = BuildEstimate(
            totalItemsCount: 4, itemsWithCostsCount: 2);

        ProjectAdditionalCostsWeb additionalCosts = BuildAdditionalCosts();

        CostTrackerSummaryWeb result = _sut.ComputeProjectSummary(
            new List<CostEstimateSummaryWeb> { estimate },
            additionalCosts, null, null);

        result.CoveredPercent.Should().Be(50m); // 2/4 * 100
    }
}
