using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class ScheduleSummaryBuilder : CostTrackerHandlerBase, IScheduleSummaryBuilder
    {
        public ScheduleSummaryBuilder(
            ICurrentUser currentUser,
            IRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService,
            ICostTrackerFinancialService financialService,
            ICostTrackerTimelineService timelineService,
            IContractorService contractorService)
            : base(currentUser, trackedCostRepository, attachmentService, financialService, timelineService, contractorService)
        {
        }

        public List<ScheduleSummaryWeb> BuildAll(
            List<WorkSchedule> schedules,
            List<WorkScheduleStage> allStages,
            List<WorkScheduleStageWork> allStageWorks,
            HashSet<Guid> closedWorkIds,
            Dictionary<Guid, CostEstimateItem> stageWorkLinkedItems,
            List<BaseCost> allCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            DateTime referenceDate,
            Dictionary<Guid, string> contractorNames)
        {
            SetContractorNames(contractorNames);

            if (schedules.Count == 0)
            {
                return new List<ScheduleSummaryWeb>();
            }

            ILookup<Guid, WorkScheduleStage> stagesByScheduleId = allStages.ToLookup(s => s.WorkScheduleId);
            ILookup<Guid, WorkScheduleStageWork> worksByStageId = allStageWorks.ToLookup(w => w.WorkScheduleStageId);

            HashSet<Guid> workIds = allStageWorks.Select(w => w.Id).ToHashSet();
            ILookup<Guid, TrackedCost> costsByStageWorkId = allCosts.OfType<TrackedCost>()
                .Where(tc => tc.WorkScheduleStageWorkId.HasValue && workIds.Contains(tc.WorkScheduleStageWorkId!.Value))
                .ToLookup(tc => tc.WorkScheduleStageWorkId!.Value);

            return schedules
                .Select(schedule => BuildScheduleSummaryWeb(
                    schedule,
                    stagesByScheduleId[schedule.Id].OrderBy(s => s.Order).ToList(),
                    worksByStageId,
                    costsByStageWorkId,
                    attachmentsByCostId,
                    closedWorkIds,
                    stageWorkLinkedItems,
                    referenceDate))
                .ToList();
        }

        private ScheduleSummaryWeb BuildScheduleSummaryWeb(
            WorkSchedule schedule,
            List<WorkScheduleStage> stages,
            ILookup<Guid, WorkScheduleStageWork> worksByStageId,
            ILookup<Guid, TrackedCost> costsByStageWorkId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            HashSet<Guid> closedWorkIds,
            Dictionary<Guid, CostEstimateItem> ceItemsById,
            DateTime referenceDate)
        {
            Dictionary<Guid, List<WorkScheduleStage>> childrenByParentId = stages
                .Where(s => s.ParentStageId.HasValue)
                .GroupBy(s => s.ParentStageId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<WorkScheduleStage> rootStages = stages
                .Where(s => s.ParentStageId is null)
                .OrderBy(s => s.Order)
                .ToList();

            List<ScheduleStageWeb> stageWebs = rootStages
                .Select(stage => BuildScheduleStageWeb(
                    stage, childrenByParentId, worksByStageId,
                    costsByStageWorkId, attachmentsByCostId, closedWorkIds, ceItemsById, referenceDate))
                .ToList();

            decimal? budgetNet = stageWebs.Any(s => s.BudgetNet.HasValue) ? stageWebs.Sum(s => s.BudgetNet ?? 0) : null;
            decimal? budgetGross = stageWebs.Any(s => s.BudgetGross.HasValue) ? stageWebs.Sum(s => s.BudgetGross ?? 0) : null;
            decimal? costsNet = stageWebs.Any(s => s.CostsNet.HasValue) ? stageWebs.Sum(s => s.CostsNet ?? 0) : null;
            decimal? costsGross = stageWebs.Any(s => s.CostsGross.HasValue) ? stageWebs.Sum(s => s.CostsGross ?? 0) : null;
            decimal? deviationNet = budgetNet.HasValue && costsNet.HasValue ? Math.Round(budgetNet.Value - costsNet.Value, 4) : null;
            decimal? deviationGross = budgetGross.HasValue && costsGross.HasValue ? Math.Round(budgetGross.Value - costsGross.Value, 4) : null;
            decimal? deviationPercent = budgetNet.HasValue && budgetNet.Value != 0 && deviationNet.HasValue
                ? Math.Round(deviationNet.Value / budgetNet.Value * 100, 4) : null;
            decimal? coveredPercent = budgetNet.HasValue && budgetNet.Value != 0 && costsNet.HasValue
                ? Math.Round(costsNet.Value / budgetNet.Value * 100, 4) : null;

            IEnumerable<TimelineStatsWeb?> stageTimelines = stageWebs.Select(s => s.Timeline);
            TimelineStatsWeb? scheduleTimeline = timelineService!.AggregateTimelineStats(stageTimelines, referenceDate);
            bool hasLinkedSchedule = scheduleTimeline is not null;

            int totalWorkItemsCount = stageWebs.Sum(s => s.TotalWorkItemsCount);
            bool hasConfiguredPeriods = stageWebs.Any(s => s.HasLinkedSchedule && s.Timeline?.PlannedStart.HasValue == true);
            TimelineStatus timelineStatus = totalWorkItemsCount == 0 || (hasLinkedSchedule && !hasConfiguredPeriods)
                ? TimelineStatus.NotConfigured
                : hasLinkedSchedule ? scheduleTimeline!.OverallStatus : TimelineStatus.NoSchedule;

            return new ScheduleSummaryWeb
            {
                WorkScheduleId           = schedule.Id,
                WorkScheduleName         = schedule.Name,
                HasLinkedEstimate        = schedule.CostEstimateId.HasValue,
                LinkedCostEstimateId     = schedule.CostEstimateId,
                TotalWorkItemsCount      = stageWebs.Sum(s => s.TotalWorkItemsCount),
                WorkItemsWithCostsCount  = stageWebs.Sum(CountWorkItemsWithCosts),
                WorkItemsOverBudgetCount = stageWebs.Sum(s => CountWorkItemsByStatus(s, FinancialStatus.OverBudget)),
                WorkItemsNearLimitCount  = stageWebs.Sum(s => CountWorkItemsByStatus(s, FinancialStatus.NearLimit)),
                WorkItemsDelayedCount    = stageWebs.Sum(s => s.DelayedWorkItemsCount),
                BudgetNet                = budgetNet,
                BudgetGross              = budgetGross,
                CostsNet                 = costsNet,
                CostsGross               = costsGross,
                DeviationNet             = deviationNet,
                DeviationGross           = deviationGross,
                DeviationPercent         = deviationPercent,
                CoveredPercent           = coveredPercent,
                IsBudgetExceeded         = budgetNet.HasValue && costsNet.HasValue && costsNet.Value > budgetNet.Value,
                CostCount                = stageWebs.Sum(s => s.CostCount),
                FinancialStatus          = financialService!.ComputeFinancialStatus(budgetNet, costsNet),
                TimelineStatus           = timelineStatus,
                HasLinkedSchedule        = hasLinkedSchedule,
                Timeline                 = scheduleTimeline,
                Stages                   = stageWebs,
                TotalWorkItemsCostsNet   = costsNet,
                TotalWorkItemsCostsGross = costsGross
            };
        }

        private static int CountWorkItemsWithCosts(ScheduleStageWeb stage)
            => stage.WorkItems.Count(i => i.CostCount > 0) + stage.ChildStages.Sum(CountWorkItemsWithCosts);

        private static int CountWorkItemsByStatus(ScheduleStageWeb stage, FinancialStatus status)
            => stage.WorkItems.Count(i => i.FinancialStatus == status) + stage.ChildStages.Sum(s => CountWorkItemsByStatus(s, status));

        private ScheduleStageWeb BuildScheduleStageWeb(
            WorkScheduleStage stage,
            Dictionary<Guid, List<WorkScheduleStage>> childrenByParentId,
            ILookup<Guid, WorkScheduleStageWork> worksByStageId,
            ILookup<Guid, TrackedCost> costsByStageWorkId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            HashSet<Guid> closedWorkIds,
            Dictionary<Guid, CostEstimateItem> ceItemsById,
            DateTime referenceDate)
        {
            List<ScheduleStageWeb> childStageWebs = childrenByParentId.TryGetValue(stage.Id, out List<WorkScheduleStage>? children)
                ? children
                    .OrderBy(c => c.Order)
                    .Select(c => BuildScheduleStageWeb(
                        c, childrenByParentId, worksByStageId,
                        costsByStageWorkId, attachmentsByCostId, closedWorkIds, ceItemsById, referenceDate))
                    .ToList()
                : new List<ScheduleStageWeb>();

            List<WorkItemLinkWeb> workItems = worksByStageId[stage.Id]
                .OrderBy(w => w.Order)
                .Select(w =>
                {
                    List<TrackedCost> resolvedCosts = costsByStageWorkId[w.Id].ToList();
                    bool isWorkClosed = closedWorkIds.Contains(w.Id);
                    CostEstimateItem? linkedItem = w.CostEstimateItemId.HasValue
                        && ceItemsById.TryGetValue(w.CostEstimateItemId.Value, out CostEstimateItem? ci) ? ci : null;

                    return BuildWorkItemLinkWebFromStageWork(
                        w, resolvedCosts, attachmentsByCostId, linkedItem, isWorkClosed, referenceDate);
                })
                .ToList();

            decimal? directBudgetNet = workItems.Any(i => i.BudgetNet.HasValue) ? workItems.Sum(i => i.BudgetNet ?? 0) : null;
            decimal? directBudgetGross = workItems.Any(i => i.BudgetGross.HasValue) ? workItems.Sum(i => i.BudgetGross ?? 0) : null;
            decimal? directCostsNet = workItems.Any(i => i.CostsNet.HasValue) ? workItems.Sum(i => i.CostsNet ?? 0) : null;
            decimal? directCostsGross = workItems.Any(i => i.CostsGross.HasValue) ? workItems.Sum(i => i.CostsGross ?? 0) : null;

            decimal? budgetNet = CombineNullableStage(directBudgetNet, childStageWebs.Any(s => s.BudgetNet.HasValue) ? childStageWebs.Sum(s => s.BudgetNet ?? 0) : null);
            decimal? budgetGross = CombineNullableStage(directBudgetGross, childStageWebs.Any(s => s.BudgetGross.HasValue) ? childStageWebs.Sum(s => s.BudgetGross ?? 0) : null);
            decimal? costsNet = CombineNullableStage(directCostsNet, childStageWebs.Any(s => s.CostsNet.HasValue) ? childStageWebs.Sum(s => s.CostsNet ?? 0) : null);
            decimal? costsGross = CombineNullableStage(directCostsGross, childStageWebs.Any(s => s.CostsGross.HasValue) ? childStageWebs.Sum(s => s.CostsGross ?? 0) : null);

            decimal? deviationNet = budgetNet.HasValue && costsNet.HasValue ? Math.Round(budgetNet.Value - costsNet.Value, 4) : null;
            decimal? deviationGross = budgetGross.HasValue && costsGross.HasValue ? Math.Round(budgetGross.Value - costsGross.Value, 4) : null;
            decimal? deviationPercent = budgetNet.HasValue && budgetNet.Value != 0 && deviationNet.HasValue
                ? Math.Round(deviationNet.Value / budgetNet.Value * 100, 4) : null;
            decimal? coveredPercent = budgetNet.HasValue && budgetNet.Value != 0 && costsNet.HasValue
                ? Math.Round(costsNet.Value / budgetNet.Value * 100, 4) : null;

            int totalWorkItems = workItems.Count + childStageWebs.Sum(s => s.TotalWorkItemsCount);
            int completedWorkItems = workItems.Count(i => i.TimelineStatus is TimelineStatus.Completed or TimelineStatus.CompletedLate)
                + childStageWebs.Sum(s => s.CompletedWorkItemsCount);
            int delayedWorkItems = workItems.Count(i => i.TimelineStatus is TimelineStatus.Delayed or TimelineStatus.CompletedLate)
                + childStageWebs.Sum(s => s.DelayedWorkItemsCount);
            int costCount = workItems.Sum(i => i.CostCount) + childStageWebs.Sum(s => s.CostCount);

            TimelineStatsWeb? ownTimeline = timelineService!.BuildTimelineStats(workItems, referenceDate);
            IEnumerable<TimelineStatsWeb?> allTimelines = childStageWebs.Select(s => s.Timeline).Append(ownTimeline);
            TimelineStatsWeb? stageTimeline = timelineService!.AggregateTimelineStats(allTimelines, referenceDate);
            bool hasLinkedSchedule = stageTimeline is not null;

            List<WorkItemLinkWeb> scheduledItems = workItems.Where(i => i.HasLinkedSchedule).ToList();
            bool hasConfiguredPeriods = scheduledItems.Any(i => i.Timeline?.PlannedStart.HasValue == true)
                || childStageWebs.Any(s => s.HasLinkedSchedule && s.Timeline?.PlannedStart.HasValue == true);
            TimelineStatus timelineStatus = totalWorkItems == 0 || (scheduledItems.Any() && !hasConfiguredPeriods)
                ? TimelineStatus.NotConfigured
                : hasLinkedSchedule ? stageTimeline!.OverallStatus : TimelineStatus.NoSchedule;

            return new ScheduleStageWeb
            {
                StageId                 = stage.Id,
                StageName               = stage.Name,
                Order                   = stage.Order,
                TotalWorkItemsCount     = totalWorkItems,
                CompletedWorkItemsCount = completedWorkItems,
                DelayedWorkItemsCount   = delayedWorkItems,
                BudgetNet               = budgetNet,
                BudgetGross             = budgetGross,
                CostsNet                = costsNet,
                CostsGross              = costsGross,
                DeviationNet            = deviationNet,
                DeviationGross          = deviationGross,
                DeviationPercent        = deviationPercent,
                CoveredPercent          = coveredPercent,
                IsBudgetExceeded        = budgetNet.HasValue && costsNet.HasValue && costsNet.Value > budgetNet.Value,
                CostCount               = costCount,
                FinancialStatus         = financialService!.ComputeFinancialStatus(budgetNet, costsNet),
                TimelineStatus          = timelineStatus,
                HasLinkedSchedule       = hasLinkedSchedule,
                Timeline                = stageTimeline,
                WorkItems               = workItems,
                ChildStages             = childStageWebs,
                TotalWorkItemsCostsNet  = directCostsNet,
                TotalWorkItemsCostsGross = directCostsGross
            };
        }

        private static decimal? CombineNullableStage(decimal? a, decimal? b)
        {
            if (!a.HasValue && !b.HasValue)
            {
                return null;
            }

            return (a ?? 0) + (b ?? 0);
        }
    }
}
