using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;

namespace Business.Implementation.Services
{
    public sealed class CostTrackerFinancialService : ICostTrackerFinancialService
    {
        private const decimal NearLimitThreshold = 0.80m;

        public (decimal? Net, decimal? Gross) Calculate(decimal? net, decimal? gross)
        {
            if (net.HasValue && gross.HasValue)
                return (Math.Round(net.Value, 2), Math.Round(gross.Value, 2));

            if (net.HasValue)
                return (Math.Round(net.Value, 2), null);

            if (gross.HasValue)
                return (null, Math.Round(gross.Value, 2));

            return (null, null);
        }

        public TrackedCostItemStatus ComputeItemStatus(decimal? budgetNet, decimal? costsNet, int costsCount)
        {
            if (costsCount == 0)
                return TrackedCostItemStatus.NoCosts;

            if (!budgetNet.HasValue || budgetNet.Value == 0)
                return TrackedCostItemStatus.NoBudget;

            if (!costsNet.HasValue)
                return TrackedCostItemStatus.InProgress;

            if (costsNet.Value > budgetNet.Value)
                return TrackedCostItemStatus.OverBudget;

            if (costsNet.Value / budgetNet.Value >= NearLimitThreshold)
                return TrackedCostItemStatus.NearLimit;

            return TrackedCostItemStatus.InProgress;
        }

        public CostTrackerSummaryWeb ComputeProjectSummary(
            IReadOnlyCollection<CostEstimateSummaryWeb> costEstimateSummaries,
            ProjectAdditionalCostsWeb projectAdditionalCosts)
        {
            decimal? totalCostsNet = costEstimateSummaries.Any(s => s.TotalCostsNet.HasValue) || projectAdditionalCosts.TotalNet.HasValue
                ? (costEstimateSummaries.Sum(s => s.TotalCostsNet ?? 0)) + (projectAdditionalCosts.TotalNet ?? 0)
                : null;

            decimal? totalCostsGross = costEstimateSummaries.Any(s => s.TotalCostsGross.HasValue) || projectAdditionalCosts.TotalGross.HasValue
                ? (costEstimateSummaries.Sum(s => s.TotalCostsGross ?? 0)) + (projectAdditionalCosts.TotalGross ?? 0)
                : null;

            decimal? totalBudgetNet = costEstimateSummaries.Any(s => s.TotalBudgetNet.HasValue)
                ? costEstimateSummaries.Sum(s => s.TotalBudgetNet ?? 0)
                : null;

            decimal? totalBudgetGross = costEstimateSummaries.Any(s => s.TotalBudgetGross.HasValue)
                ? costEstimateSummaries.Sum(s => s.TotalBudgetGross ?? 0)
                : null;

            decimal? totalDeviationNet = totalBudgetNet.HasValue && totalCostsNet.HasValue
                ? Math.Round(totalCostsNet.Value - totalBudgetNet.Value, 2)
                : null;

            decimal? totalDeviationGross = totalBudgetGross.HasValue && totalCostsGross.HasValue
                ? Math.Round(totalCostsGross.Value - totalBudgetGross.Value, 2)
                : null;

            decimal? totalDeviationPercent = totalBudgetNet.HasValue && totalBudgetNet.Value != 0 && totalCostsNet.HasValue
                ? Math.Round((totalCostsNet.Value - totalBudgetNet.Value) / totalBudgetNet.Value * 100, 2)
                : null;

            int estimatesWithCosts = costEstimateSummaries.Count(s => s.TotalCostsNet.HasValue && s.TotalCostsNet.Value > 0);

            int costCount = costEstimateSummaries.Sum(s => s.CostCount) + projectAdditionalCosts.CostsCount;
            int totalItemsAcrossEstimates = costEstimateSummaries.Sum(s => s.TotalItemsCount);
            int itemsWithCostsAcrossEstimates = costEstimateSummaries.Sum(s => s.ItemsWithCostsCount);
            decimal? coveredPercent = totalItemsAcrossEstimates > 0
                ? Math.Round((decimal)itemsWithCostsAcrossEstimates / totalItemsAcrossEstimates * 100, 2)
                : null;

            return new CostTrackerSummaryWeb
            {
                TotalCostsNet = totalCostsNet.HasValue ? Math.Round(totalCostsNet.Value, 2) : null,
                TotalCostsGross = totalCostsGross.HasValue ? Math.Round(totalCostsGross.Value, 2) : null,
                TotalBudgetNet = totalBudgetNet,
                TotalBudgetGross = totalBudgetGross,
                TotalDeviationNet = totalDeviationNet,
                TotalDeviationGross = totalDeviationGross,
                TotalDeviationPercent = totalDeviationPercent,
                IsBudgetExceeded = totalDeviationNet.HasValue && totalDeviationNet.Value > 0,
                CostEstimatesCount = costEstimateSummaries.Count,
                CostEstimatesWithCostsCount = estimatesWithCosts,
                AdditionalCostsNet = projectAdditionalCosts.TotalNet,
                AdditionalCostsGross = projectAdditionalCosts.TotalGross,
                AdditionalCostsCount = projectAdditionalCosts.CostsCount,
                CostCount = costCount,
                CoveredPercent = coveredPercent
            };
        }

        public CostEstimateSummaryWeb ComputeEstimateSummary(
            CostEstimate costEstimate,
            IReadOnlyCollection<CostEstimateItem> budgetItems,
            ILookup<Guid, TrackedCost> costsByItemId,
            decimal? additionalCostsNet,
            decimal? additionalCostsGross,
            int additionalCostsCount)
        {
            decimal? budgetNet = costEstimate.TotalNet;
            decimal? budgetGross = costEstimate.TotalGross;

            List<TrackedCost> allItemCosts = budgetItems.SelectMany(i => costsByItemId[i.Id]).ToList();

            decimal? itemCostsNet = allItemCosts.Any(c => c.Net.HasValue)
                ? allItemCosts.Sum(c => c.Net ?? 0)
                : null;

            decimal? itemCostsGross = allItemCosts.Any(c => c.Gross.HasValue)
                ? allItemCosts.Sum(c => c.Gross ?? 0)
                : null;

            decimal? totalCostsNet = itemCostsNet.HasValue || additionalCostsNet.HasValue
                ? (itemCostsNet ?? 0) + (additionalCostsNet ?? 0)
                : null;

            decimal? totalCostsGross = itemCostsGross.HasValue || additionalCostsGross.HasValue
                ? (itemCostsGross ?? 0) + (additionalCostsGross ?? 0)
                : null;

            decimal? deviationNet = budgetNet.HasValue && totalCostsNet.HasValue
                ? Math.Round(totalCostsNet.Value - budgetNet.Value, 2)
                : null;

            decimal? deviationGross = budgetGross.HasValue && totalCostsGross.HasValue
                ? Math.Round(totalCostsGross.Value - budgetGross.Value, 2)
                : null;

            decimal? deviationPercent = budgetNet.HasValue && budgetNet.Value != 0 && totalCostsNet.HasValue
                ? Math.Round((totalCostsNet.Value - budgetNet.Value) / budgetNet.Value * 100, 2)
                : null;

            int totalItems = budgetItems.Count;
            int itemsWithCosts = budgetItems.Count(i => costsByItemId[i.Id].Any());
            int itemsWithoutCosts = totalItems - itemsWithCosts;

            int itemsOverBudget = budgetItems.Count(i =>
            {
                List<TrackedCost> perItemCostsList = costsByItemId[i.Id].ToList();
                decimal? perItemCostsNet = perItemCostsList.Any(c => c.Net.HasValue) ? perItemCostsList.Sum(c => c.Net ?? 0) : null;
                return ComputeItemStatus(i.NetValue, perItemCostsNet, perItemCostsList.Count) == TrackedCostItemStatus.OverBudget;
            });

            int itemsNearLimit = budgetItems.Count(i =>
            {
                List<TrackedCost> perItemCostsList = costsByItemId[i.Id].ToList();
                decimal? perItemCostsNet = perItemCostsList.Any(c => c.Net.HasValue) ? perItemCostsList.Sum(c => c.Net ?? 0) : null;
                return ComputeItemStatus(i.NetValue, perItemCostsNet, perItemCostsList.Count) == TrackedCostItemStatus.NearLimit;
            });

            decimal? coveragePercent = totalItems > 0
                ? Math.Round((decimal)itemsWithCosts / totalItems * 100, 2)
                : null;

            int itemCostsCount = budgetItems.Sum(i => costsByItemId[i.Id].Count());
            int costCount = itemCostsCount + additionalCostsCount;

            return new CostEstimateSummaryWeb
            {
                CostEstimateId = costEstimate.Id,
                CostEstimateName = costEstimate.Name,
                TotalBudgetNet = budgetNet,
                TotalBudgetGross = budgetGross,
                TotalCostsNet = totalCostsNet.HasValue ? Math.Round(totalCostsNet.Value, 2) : null,
                TotalCostsGross = totalCostsGross.HasValue ? Math.Round(totalCostsGross.Value, 2) : null,
                TotalDeviationNet = deviationNet,
                TotalDeviationGross = deviationGross,
                TotalDeviationPercent = deviationPercent,
                IsBudgetExceeded = deviationNet.HasValue && deviationNet.Value > 0,
                TotalItemsCount = totalItems,
                ItemsWithCostsCount = itemsWithCosts,
                ItemsWithoutCostsCount = itemsWithoutCosts,
                ItemsOverBudgetCount = itemsOverBudget,
                ItemsNearLimitCount = itemsNearLimit,
                AdditionalCostsNet = additionalCostsNet,
                AdditionalCostsGross = additionalCostsGross,
                AdditionalCostsCount = additionalCostsCount,
                CostCount = costCount,
                CoveredPercent = coveragePercent,
                Groups = [],
                AdditionalCosts = new TrackerAdditionalCostsWeb
                {
                    TotalNet = additionalCostsNet,
                    TotalGross = additionalCostsGross,
                    CostsCount = additionalCostsCount,
                    Costs = []
                }
            };
        }
    }
}
