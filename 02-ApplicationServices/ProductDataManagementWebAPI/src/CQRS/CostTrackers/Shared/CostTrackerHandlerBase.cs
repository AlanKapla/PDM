using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class CostTrackerHandlerBase
    {
        private readonly IRepository<TrackedCost> trackedCostRepository;
        protected readonly ICostTrackerAttachmentService attachmentService;
        protected readonly ICurrentUser currentUser;
        protected readonly ICostTrackerFinancialService? financialService;
        protected readonly ICostTrackerTimelineService? timelineService;
        protected readonly IContractorService contractorService;
        private Dictionary<Guid, string> contractorNamesCache = new Dictionary<Guid, string>();

        protected CostTrackerHandlerBase(
            ICurrentUser currentUser,
            IRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService,
            IContractorService contractorService)
        {
            this.currentUser = currentUser;
            this.trackedCostRepository = trackedCostRepository;
            this.attachmentService = attachmentService;
            this.contractorService = contractorService;
        }

        protected CostTrackerHandlerBase(
            ICurrentUser currentUser,
            IRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService,
            ICostTrackerFinancialService financialService,
            ICostTrackerTimelineService timelineService,
            IContractorService contractorService)
            : this(currentUser, trackedCostRepository, attachmentService, contractorService)
        {
            this.financialService = financialService;
            this.timelineService = timelineService;
        }

        protected CostTrackerHandlerBase(
            ICurrentUser currentUser,
            IRepository<TrackedCost> trackedCostRepository,
            IContractorService contractorService)
        {
            this.currentUser = currentUser;
            this.trackedCostRepository = trackedCostRepository;
            this.contractorService = contractorService;
        }

        protected async Task ValidateAccessAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);
            if (!isAdmin)
            {
                throw new ForbiddenApiException("You do not have access to this resource.");
            }
        }

        protected async Task<TrackedCost> GetAndValidateTrackedCostAsync(
            Guid costId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            return await trackedCostRepository.GetFirstBySearch(
                tc => tc.Id == costId && tc.TenantId == tenantId && tc.ProjectId == projectId)
                ?? throw new NotFoundApiException(nameof(TrackedCost), costId.ToString());
        }

        protected TrackedCostWeb MapTrackedCostToWeb(
            TrackedCost cost,
            IEnumerable<BaseCostAttachment> attachments)
        {
            List<TrackedCostAttachmentWeb> attachmentWebs = attachments
                .Select(a => new TrackedCostAttachmentWeb
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileUrl = attachmentService.GenerateFileUrl(a),
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            return new TrackedCostWeb
            {
                Id = cost.Id,
                CostEstimateItemId = cost.CostEstimateItemId,
                WorkScheduleStageWorkId = cost.WorkScheduleStageWorkId,
                IsAdditional = !cost.CostEstimateItemId.HasValue && !cost.WorkScheduleStageWorkId.HasValue,
                SourceType = ResolveSourceType(cost),
                Name = cost.Name,
                Number = cost.Number,
                Description = cost.Description,
                Net = cost.Net,
                Gross = cost.Gross,
                ContractorId = cost.ContractorId,
                ContractorName = cost.ContractorId.HasValue
                    ? contractorNamesCache.GetValueOrDefault(cost.ContractorId.Value)
                    : null,
                Date = cost.Date,
                CreatedAt = cost.CreatedAt,
                UpdatedAt = cost.UpdatedAt,
                Attachments = attachmentWebs
            };
        }

        protected async Task<Dictionary<Guid, string>> LoadContractorNamesAsync(
            IEnumerable<BaseCost> costs, Guid tenantId, CancellationToken cancellationToken)
        {
            List<Guid> ids = costs
                .Where(c => c.ContractorId.HasValue)
                .Select(c => c.ContractorId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, string> names = ids.Count > 0
                ? await contractorService.GetNamesByIdsAsync(ids, tenantId, cancellationToken)
                : new Dictionary<Guid, string>();

            contractorNamesCache = names;
            return names;
        }

        protected void SetContractorNames(Dictionary<Guid, string> names)
        {
            contractorNamesCache = names;
        }

        protected WorkItemLinkWeb BuildWorkItemLinkWebFromStageWork(
            WorkScheduleStageWork work,
            List<TrackedCost> costs,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            CostEstimateItem? linkedItem,
            bool isWorkClosed,
            DateTime referenceDate)
        {
            TimelineStatsWeb timeline = BuildLeafTimelineStats(
                work.PlannedStartDate, work.PlannedEndDate, isWorkClosed, referenceDate);

            decimal? costsNet = costs.Any(c => c.Net.HasValue) ? costs.Sum(c => c.Net ?? 0) : null;
            decimal? costsGross = costs.Any(c => c.Gross.HasValue) ? costs.Sum(c => c.Gross ?? 0) : null;
            decimal? budgetNet = linkedItem?.NetValue;
            decimal? budgetGross = linkedItem?.GrossValue;
            decimal? deviationNet = budgetNet.HasValue && costsNet.HasValue
                ? Math.Round(budgetNet.Value - costsNet.Value, 2) : null;
            decimal? deviationPercent = budgetNet.HasValue && budgetNet.Value != 0 && deviationNet.HasValue
                ? Math.Round(deviationNet.Value / budgetNet.Value * 100, 2) : null;
            WorkItemType workItemType = linkedItem is not null ? WorkItemType.LinkedWorkItem : WorkItemType.ScheduleWorkItem;

            List<TrackedCostWeb> costWebs = costs
                .Select(c => MapTrackedCostToWeb(c, attachmentsByCostId[c.Id]))
                .ToList();

            return new WorkItemLinkWeb
            {
                DisplayName = work.Name,
                Order = work.Order,
                WorkItemType = workItemType,
                CostEstimateItemId = work.CostEstimateItemId,
                WorkScheduleStageWorkId = work.Id,
                BudgetNet = budgetNet,
                BudgetGross = budgetGross,
                CostsNet = costsNet,
                CostsGross = costsGross,
                DeviationNet = deviationNet,
                DeviationPercent = deviationPercent,
                IsBudgetExceeded = deviationNet.HasValue && deviationNet.Value < 0,
                FinancialStatus = financialService!.ComputeItemStatus(budgetNet, costsNet, costs.Count),
                TimelineStatus = timeline.OverallStatus,
                CostCount = costs.Count,
                CoveredPercent = costs.Count > 0 ? 100.0m : 0.0m,
                BudgetCoveredPercent = budgetNet.HasValue && budgetNet.Value != 0 && costsNet.HasValue
                    ? Math.Round(costsNet.Value / budgetNet.Value * 100, 2) : null,
                HasLinkedSchedule = true,
                Timeline = timeline,
                TimelinePlannedStart = ToDateOnly(timeline.PlannedStart),
                TimelinePlannedEnd = ToDateOnly(timeline.PlannedEnd),
                TimelineTotalDays = timeline.TotalPlannedDays.HasValue
                    ? (int?)Math.Round(timeline.TotalPlannedDays.Value) : null,
                Costs = costWebs
            };
        }

        protected static TimelineStatsWeb BuildLeafTimelineStats(
            DateTime? plannedStart, DateTime? plannedEnd, bool isWorkClosed, DateTime referenceDate)
        {
            TimelineStatus status;

            if (!plannedStart.HasValue)
                status = TimelineStatus.NotConfigured;
            else if (isWorkClosed)
                status = plannedEnd.HasValue && referenceDate > plannedEnd.Value
                    ? TimelineStatus.CompletedLate
                    : TimelineStatus.Completed;
            else if (referenceDate < plannedStart.Value)
                status = TimelineStatus.NotStarted;
            else if (!plannedEnd.HasValue || referenceDate <= plannedEnd.Value)
                status = TimelineStatus.InProgress;
            else
                status = TimelineStatus.Delayed;

            double? totalDays = plannedStart.HasValue && plannedEnd.HasValue
                ? (plannedEnd.Value - plannedStart.Value).TotalDays
                : null;

            bool hasWork = plannedStart.HasValue;
            int completedCount     = status == TimelineStatus.Completed ? 1 : 0;
            int completedLateCount = status == TimelineStatus.CompletedLate ? 1 : 0;
            int inProgressCount    = status == TimelineStatus.InProgress ? 1 : 0;
            int notStartedCount    = status == TimelineStatus.NotStarted ? 1 : 0;
            int delayedCount       = status == TimelineStatus.Delayed ? 1 : 0;

            decimal? progressPercent = hasWork
                ? completedCount + completedLateCount > 0 ? 100.0m : 0.0m
                : null;

            double? delayDays = plannedEnd.HasValue &&
                (status == TimelineStatus.Delayed || status == TimelineStatus.CompletedLate)
                ? (referenceDate - plannedEnd.Value).TotalDays
                : null;

            return new TimelineStatsWeb
            {
                PlannedStart = plannedStart,
                PlannedEnd = plannedEnd,
                TotalPlannedDays = totalDays,
                TotalWorkCount = hasWork ? 1 : 0,
                CompletedCount = completedCount,
                CompletedLateCount = completedLateCount,
                InProgressCount = inProgressCount,
                NotStartedCount = notStartedCount,
                DelayedCount = delayedCount,
                ProgressPercent = progressPercent,
                DelayDays = delayDays,
                OverallStatus = status,
                IsDelayed = status == TimelineStatus.Delayed || status == TimelineStatus.CompletedLate,
                IsCompleted = status == TimelineStatus.Completed || status == TimelineStatus.CompletedLate
            };
        }

        protected CostEstimateSummaryWeb BuildEstimateSummary(
            CostEstimate costEstimate,
            Dictionary<Guid, CostEstimateItem> itemsDict,
            ILookup<Guid, TrackedCost> costsByItemId,
            List<TrackedCost> additionalCostsList,
            List<TrackerGroupWeb> groups,
            List<TrackedCostWeb> additionalCostWebs,
            DateTime referenceDate,
            Guid? linkedWorkScheduleId)
        {
            decimal? additionalNet = additionalCostsList.Any(c => c.Net.HasValue)
                ? additionalCostsList.Sum(c => c.Net ?? 0)
                : null;

            decimal? additionalGross = additionalCostsList.Any(c => c.Gross.HasValue)
                ? additionalCostsList.Sum(c => c.Gross ?? 0)
                : null;

            List<CostEstimateItem> allMainItems = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .ToList();

            CostEstimateSummaryWeb summary = financialService!.ComputeEstimateSummary(
                costEstimate: costEstimate,
                budgetItems: allMainItems,
                costsByItemId: costsByItemId,
                additionalCostsNet: additionalNet,
                additionalCostsGross: additionalGross,
                additionalCostsCount: additionalCostsList.Count);

            TimelineStatsWeb? estimateTimeline = timelineService!.AggregateTimelineStats(
                groups.Select(g => g.Timeline), referenceDate);
            bool hasLinkedSchedule = groups.Any(g => g.HasLinkedSchedule);

            decimal? estimateBudgetCoveredPercent = summary.BudgetNet.HasValue && summary.BudgetNet.Value != 0 && summary.CostsNet.HasValue
                ? Math.Round(summary.CostsNet.Value / summary.BudgetNet.Value * 100, 2) : null;

            return summary with
            {
                LinkedWorkScheduleId = linkedWorkScheduleId,
                HasLinkedSchedule = hasLinkedSchedule,
                Timeline = estimateTimeline,
                TimelineStatus = estimateTimeline?.OverallStatus ?? TimelineStatus.NoSchedule,
                Groups = groups,
                BudgetCoveredPercent = estimateBudgetCoveredPercent,
                TimelinePlannedStart = ToDateOnly(estimateTimeline?.PlannedStart),
                TimelinePlannedEnd = ToDateOnly(estimateTimeline?.PlannedEnd),
                TimelineTotalDays = estimateTimeline?.TotalPlannedDays.HasValue == true
                    ? (int?)Math.Round(estimateTimeline.TotalPlannedDays.Value) : null
            };
        }

        private static DateOnly? ToDateOnly(DateTime? dt) =>
            dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null;

        protected List<TrackerGroupWeb> BuildTrackerGroups(
            Dictionary<Guid, CostEstimateGroup> groupsDict,
            Dictionary<Guid, CostEstimateItem> itemsDict,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            ILookup<Guid, Entities.Models.WorkSchedules.WorkScheduleStageWork> stageWorksByItemId,
            HashSet<Guid> closedWorkIds,
            DateTime referenceDate)
        {
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId = groupsDict.Values
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            List<CostEstimateGroup> rootGroups = groupsDict.Values
                .Where(g => g.ParentGroupId == null)
                .OrderBy(g => g.Order)
                .ToList();

            return BuildTrackerGroupHierarchy(
                rootGroups,
                childGroupsByParentId,
                mainItemsByGroupId,
                costsByItemId,
                attachmentsByCostId,
                stageWorksByItemId,
                closedWorkIds,
                referenceDate);
        }

        private List<TrackerGroupWeb> BuildTrackerGroupHierarchy(
            List<CostEstimateGroup> currentLevelGroups,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            ILookup<Guid, Entities.Models.WorkSchedules.WorkScheduleStageWork> stageWorksByItemId,
            HashSet<Guid> closedWorkIds,
            DateTime referenceDate)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group => BuildTrackerGroupWeb(
                    group, childGroupsByParentId, mainItemsByGroupId,
                    costsByItemId, attachmentsByCostId, stageWorksByItemId, closedWorkIds, referenceDate))
                .ToList();
        }

        private TrackerGroupWeb BuildTrackerGroupWeb(
            CostEstimateGroup group,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            ILookup<Guid, Entities.Models.WorkSchedules.WorkScheduleStageWork> stageWorksByItemId,
            HashSet<Guid> closedWorkIds,
            DateTime referenceDate)
        {
            List<CostEstimateGroup> children = childGroupsByParentId.TryGetValue(group.Id, out List<CostEstimateGroup>? childList)
                ? childList : [];

            List<TrackerGroupWeb> childGroups = BuildTrackerGroupHierarchy(
                children, childGroupsByParentId, mainItemsByGroupId,
                costsByItemId, attachmentsByCostId, stageWorksByItemId, closedWorkIds, referenceDate);

            List<CostEstimateItem> groupItems = mainItemsByGroupId.TryGetValue(group.Id, out List<CostEstimateItem>? items)
                ? items : [];

            List<WorkItemLinkWeb> workItemLinks = groupItems
                .Select(item => BuildWorkItemLinkWebForCostEstimateItem(
                    item, stageWorksByItemId, closedWorkIds, costsByItemId, attachmentsByCostId, referenceDate))
                .ToList();

            decimal? directCostsNet = workItemLinks.Any(i => i.CostsNet.HasValue) ? workItemLinks.Sum(i => i.CostsNet ?? 0) : null;
            decimal? directCostsGross = workItemLinks.Any(i => i.CostsGross.HasValue) ? workItemLinks.Sum(i => i.CostsGross ?? 0) : null;
            decimal? directBudgetNet = workItemLinks.Any(i => i.BudgetNet.HasValue) ? workItemLinks.Sum(i => i.BudgetNet ?? 0) : null;
            decimal? directBudgetGross = workItemLinks.Any(i => i.BudgetGross.HasValue) ? workItemLinks.Sum(i => i.BudgetGross ?? 0) : null;

            decimal? groupCostsNet = CombineNullable(directCostsNet, AggregateNullable(childGroups.Select(g => g.CostsNet)));
            decimal? groupCostsGross = CombineNullable(directCostsGross, AggregateNullable(childGroups.Select(g => g.CostsGross)));
            decimal? groupBudgetNet = CombineNullable(directBudgetNet, AggregateNullable(childGroups.Select(g => g.BudgetNet)));
            decimal? groupBudgetGross = CombineNullable(directBudgetGross, AggregateNullable(childGroups.Select(g => g.BudgetGross)));

            decimal? groupDeviationNet = groupBudgetNet.HasValue && groupCostsNet.HasValue
                ? Math.Round(groupBudgetNet.Value - groupCostsNet.Value, 2) : null;
            decimal? groupDeviationGross = groupBudgetGross.HasValue && groupCostsGross.HasValue
                ? Math.Round(groupBudgetGross.Value - groupCostsGross.Value, 2) : null;
            decimal? groupDeviationPercent = groupBudgetNet.HasValue && groupBudgetNet.Value != 0 && groupDeviationNet.HasValue
                ? Math.Round(groupDeviationNet.Value / groupBudgetNet.Value * 100, 2) : null;

            int groupCostCount = workItemLinks.Sum(i => i.CostCount) + childGroups.Sum(g => g.CostCount);
            FinancialStatus groupStatus = financialService!.ComputeFinancialStatus(groupBudgetNet, groupCostsNet);

            int directTotalItems = workItemLinks.Count;
            int totalItemsInGroup = directTotalItems + childGroups.Sum(g => g.TotalItemsCount);

            int directItemsWithCosts = workItemLinks.Count(i => i.CostCount > 0);
            int itemsWithCostsInGroup = directItemsWithCosts + childGroups.Sum(g => g.ItemsWithCostsCount);
            int itemsWithoutCostsInGroup = (directTotalItems - directItemsWithCosts) + childGroups.Sum(g => g.ItemsWithoutCostsCount);
            int itemsOverBudgetInGroup = workItemLinks.Count(i => i.FinancialStatus == FinancialStatus.OverBudget)
                + childGroups.Sum(g => g.ItemsOverBudgetCount);
            int itemsNearLimitInGroup = workItemLinks.Count(i => i.FinancialStatus == FinancialStatus.NearLimit)
                + childGroups.Sum(g => g.ItemsNearLimitCount);

            decimal? groupCoveredPercent = totalItemsInGroup > 0
                ? Math.Round((decimal)itemsWithCostsInGroup / totalItemsInGroup * 100, 2) : null;

            IEnumerable<TimelineStatsWeb?> allTimelines = workItemLinks.Select(i => i.Timeline)
                .Concat(childGroups.Select(g => g.Timeline));
            TimelineStatsWeb? groupTimeline = timelineService!.AggregateTimelineStats(allTimelines, referenceDate);
            bool hasLinkedSchedule = workItemLinks.Any(i => i.HasLinkedSchedule) || childGroups.Any(g => g.HasLinkedSchedule);
            TimelineStatus groupTimelineStatus = groupTimeline?.OverallStatus ?? TimelineStatus.NoSchedule;

            return new TrackerGroupWeb
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Order = group.Order,
                BudgetNet = groupBudgetNet,
                BudgetGross = groupBudgetGross,
                CostsNet = groupCostsNet,
                CostsGross = groupCostsGross,
                DeviationNet = groupDeviationNet,
                DeviationGross = groupDeviationGross,
                DeviationPercent = groupDeviationPercent,
                IsBudgetExceeded = groupDeviationNet.HasValue && groupDeviationNet.Value < 0,
                FinancialStatus = groupStatus,
                TimelineStatus = groupTimelineStatus,
                CostCount = groupCostCount,
                CoveredPercent = groupCoveredPercent,
                BudgetCoveredPercent = groupBudgetNet.HasValue && groupBudgetNet.Value != 0 && groupCostsNet.HasValue
                    ? Math.Round(groupCostsNet.Value / groupBudgetNet.Value * 100, 2) : null,
                TotalItemsCount = totalItemsInGroup,
                ItemsWithCostsCount = itemsWithCostsInGroup,
                ItemsWithoutCostsCount = itemsWithoutCostsInGroup,
                ItemsOverBudgetCount = itemsOverBudgetInGroup,
                ItemsNearLimitCount = itemsNearLimitInGroup,
                Items = workItemLinks,
                ChildGroups = childGroups,
                HasLinkedSchedule = hasLinkedSchedule,
                Timeline = groupTimeline,
                TimelinePlannedStart = ToDateOnly(groupTimeline?.PlannedStart),
                TimelinePlannedEnd = ToDateOnly(groupTimeline?.PlannedEnd),
                TimelineTotalDays = groupTimeline?.TotalPlannedDays.HasValue == true
                    ? (int?)Math.Round(groupTimeline.TotalPlannedDays.Value) : null
            };
        }

        private WorkItemLinkWeb BuildWorkItemLinkWebForCostEstimateItem(
            CostEstimateItem item,
            ILookup<Guid, Entities.Models.WorkSchedules.WorkScheduleStageWork> stageWorksByItemId,
            HashSet<Guid> closedWorkIds,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            DateTime referenceDate)
        {
            Entities.Models.WorkSchedules.WorkScheduleStageWork? stageWork = stageWorksByItemId[item.Id].FirstOrDefault();
            List<TrackedCost> costs = costsByItemId[item.Id].ToList();

            decimal? costsNet = costs.Any(c => c.Net.HasValue) ? costs.Sum(c => c.Net ?? 0) : null;
            decimal? costsGross = costs.Any(c => c.Gross.HasValue) ? costs.Sum(c => c.Gross ?? 0) : null;
            decimal? deviationNet = item.NetValue.HasValue && costsNet.HasValue
                ? Math.Round(item.NetValue.Value - costsNet.Value, 2) : null;
            decimal? deviationPercent = item.NetValue.HasValue && item.NetValue.Value != 0 && deviationNet.HasValue
                ? Math.Round(deviationNet.Value / item.NetValue.Value * 100, 2) : null;

            bool isWorkClosed = stageWork is not null && closedWorkIds.Contains(stageWork.Id);
            TimelineStatsWeb? timeline = stageWork is not null
                ? BuildLeafTimelineStats(stageWork.PlannedStartDate, stageWork.PlannedEndDate, isWorkClosed, referenceDate)
                : null;

            List<TrackedCostWeb> costWebs = costs
                .Select(c => MapTrackedCostToWeb(c, attachmentsByCostId[c.Id]))
                .ToList();

            return new WorkItemLinkWeb
            {
                DisplayName = stageWork?.Name ?? item.Name,
                Order = stageWork?.Order ?? item.Order,
                WorkItemType = stageWork is not null ? WorkItemType.LinkedWorkItem : WorkItemType.EstimateItem,
                CostEstimateItemId = item.Id,
                WorkScheduleStageWorkId = stageWork?.Id,
                BudgetNet = item.NetValue,
                BudgetGross = item.GrossValue,
                CostsNet = costsNet,
                CostsGross = costsGross,
                DeviationNet = deviationNet,
                DeviationPercent = deviationPercent,
                IsBudgetExceeded = deviationNet.HasValue && deviationNet.Value < 0,
                FinancialStatus = financialService!.ComputeItemStatus(item.NetValue, costsNet, costs.Count),
                TimelineStatus = timeline?.OverallStatus ?? TimelineStatus.NoSchedule,
                CostCount = costs.Count,
                CoveredPercent = costs.Count > 0 ? 100.0m : 0.0m,
                BudgetCoveredPercent = item.NetValue.HasValue && item.NetValue.Value != 0 && costsNet.HasValue
                    ? Math.Round(costsNet.Value / item.NetValue.Value * 100, 2) : null,
                HasLinkedSchedule = stageWork is not null,
                Timeline = timeline,
                TimelinePlannedStart = ToDateOnly(timeline?.PlannedStart),
                TimelinePlannedEnd = ToDateOnly(timeline?.PlannedEnd),
                TimelineTotalDays = timeline?.TotalPlannedDays.HasValue == true
                    ? (int?)Math.Round(timeline.TotalPlannedDays.Value) : null,
                Costs = costWebs
            };
        }

        protected static CostSourceType ResolveSourceType(TrackedCost cost)
        {
            if (cost.CostEstimateItemId.HasValue && cost.WorkScheduleStageWorkId.HasValue)
            {
                return CostSourceType.LinkedWorkItem;
            }

            if (cost.WorkScheduleStageWorkId.HasValue)
            {
                return CostSourceType.ScheduleWorkItem;
            }

            if (cost.CostEstimateItemId.HasValue)
            {
                return CostSourceType.EstimateItem;
            }

            return CostSourceType.ProjectAdditional;
        }

        protected TrackedCostWeb MapProjectCostToWeb(
            ProjectCost cost,
            IEnumerable<BaseCostAttachment> attachments)
        {
            List<TrackedCostAttachmentWeb> attachmentWebs = attachments
                .Select(a => new TrackedCostAttachmentWeb
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileUrl = attachmentService.GenerateFileUrl(a),
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            return new TrackedCostWeb
            {
                Id = cost.Id,
                CostEstimateItemId = null,
                WorkScheduleStageWorkId = null,
                IsAdditional = true,
                SourceType = CostSourceType.ProjectAdditional,
                Name = cost.Name,
                Number = cost.Number,
                Description = cost.Description,
                Net = cost.Net,
                Gross = cost.Gross,
                ContractorId = cost.ContractorId,
                ContractorName = cost.ContractorId.HasValue
                    ? contractorNamesCache.GetValueOrDefault(cost.ContractorId.Value)
                    : null,
                Date = cost.Date,
                CreatedAt = cost.CreatedAt,
                UpdatedAt = cost.UpdatedAt,
                Attachments = attachmentWebs
            };
        }

        private static decimal? CombineNullable(decimal? a, decimal? b)
        {
            if (!a.HasValue && !b.HasValue)
            {
                return null;
            }

            return (a ?? 0) + (b ?? 0);
        }

        private static decimal? AggregateNullable(IEnumerable<decimal?> values)
        {
            List<decimal?> list = values.ToList();

            if (!list.Any(v => v.HasValue))
            {
                return null;
            }

            return list.Sum(v => v ?? 0);
        }

        protected static ProjectFinancialSummaryWeb BuildProjectFinancialSummary(
            IReadOnlyCollection<CostEstimateSummaryWeb> estimateSummaries,
            ProjectAdditionalCostsWeb projectAdditionalCosts,
            IReadOnlyCollection<BaseCost> allCosts,
            decimal? projectReserveBudgetNet,
            decimal? projectReserveBudgetGross,
            int workSchedulesCount)
        {
            decimal? estimateBudgetNet = estimateSummaries.Any(s => s.BudgetNet.HasValue)
                ? estimateSummaries.Sum(s => s.BudgetNet ?? 0) : null;
            decimal? estimateBudgetGross = estimateSummaries.Any(s => s.BudgetGross.HasValue)
                ? estimateSummaries.Sum(s => s.BudgetGross ?? 0) : null;

            decimal? totalBudgetNet = estimateBudgetNet.HasValue || projectReserveBudgetNet.HasValue
                ? (estimateBudgetNet ?? 0) + (projectReserveBudgetNet ?? 0) : null;
            decimal? totalBudgetGross = estimateBudgetGross.HasValue || projectReserveBudgetGross.HasValue
                ? (estimateBudgetGross ?? 0) + (projectReserveBudgetGross ?? 0) : null;

            decimal? linkedCostsNet = estimateSummaries.Any(s => s.CostsNet.HasValue)
                ? estimateSummaries.Sum(s => s.CostsNet ?? 0) : null;
            decimal? linkedCostsGross = estimateSummaries.Any(s => s.CostsGross.HasValue)
                ? estimateSummaries.Sum(s => s.CostsGross ?? 0) : null;

            decimal? additionalCostsNet = projectAdditionalCosts.TotalNet;
            decimal? additionalCostsGross = projectAdditionalCosts.TotalGross;

            decimal? totalCostsNet = allCosts.Any(s => s.Net.HasValue)
                ? allCosts.Sum(s => s.Net ?? 0) : null;
            decimal? totalCostsGross = allCosts.Any(s => s.Gross.HasValue)
                ? allCosts.Sum(s => s.Gross ?? 0) : null;

            decimal? deviationNet = totalBudgetNet.HasValue && totalCostsNet.HasValue
                ? Math.Round(totalBudgetNet.Value - totalCostsNet.Value, 2) : null;
            decimal? deviationGross = totalBudgetGross.HasValue && totalCostsGross.HasValue
                ? Math.Round(totalBudgetGross.Value - totalCostsGross.Value, 2) : null;
            decimal? deviationPercent = totalBudgetNet.HasValue && totalBudgetNet.Value != 0 && deviationNet.HasValue
                ? Math.Round(deviationNet.Value / totalBudgetNet.Value * 100, 2) : null;
            decimal? coveredPercent = totalBudgetNet.HasValue && totalBudgetNet.Value != 0 && totalCostsNet.HasValue
                ? Math.Round(totalCostsNet.Value / totalBudgetNet.Value * 100, 2) : null;

            int linkedCostCount = estimateSummaries.Sum(s => s.CostCount);
            int additionalCostCount = projectAdditionalCosts.CostsCount;

            FinancialStatus status;
            if (!totalBudgetNet.HasValue || totalBudgetNet.Value == 0)
                status = FinancialStatus.NoBudget;
            else if (!totalCostsNet.HasValue)
                status = FinancialStatus.NoCosts;
            else if (totalCostsNet.Value > totalBudgetNet.Value)
                status = FinancialStatus.OverBudget;
            else if (totalCostsNet.Value / totalBudgetNet.Value >= 0.80m)
                status = FinancialStatus.NearLimit;
            else
                status = FinancialStatus.InProgress;

            return new ProjectFinancialSummaryWeb
            {
                TotalBudgetNet = totalBudgetNet,
                TotalBudgetGross = totalBudgetGross,
                EstimateBudgetNet = estimateBudgetNet,
                EstimateBudgetGross = estimateBudgetGross,
                ProjectReserveBudgetNet = projectReserveBudgetNet,
                ProjectReserveBudgetGross = projectReserveBudgetGross,
                TotalCostsNet = totalCostsNet,
                TotalCostsGross = totalCostsGross,
                LinkedCostsNet = linkedCostsNet,
                LinkedCostsGross = linkedCostsGross,
                AdditionalCostsNet = additionalCostsNet,
                AdditionalCostsGross = additionalCostsGross,
                DeviationNet = deviationNet,
                DeviationGross = deviationGross,
                DeviationPercent = deviationPercent,
                CoveredPercent = coveredPercent,
                IsBudgetExceeded = deviationNet.HasValue && deviationNet.Value > 0,
                FinancialStatus = status,
                TotalCostCount = allCosts.Count,
                LinkedCostCount = linkedCostCount,
                AdditionalCostCount = additionalCostCount,
                CostEstimatesCount = estimateSummaries.Count,
                CostEstimatesWithCostsCount = estimateSummaries.Count(s => s.CostsNet.HasValue && s.CostsNet.Value > 0),
                CostEstimatesOverBudgetCount = estimateSummaries.Count(s => s.IsBudgetExceeded),
                WorkSchedulesCount = workSchedulesCount
            };
        }
    }
}
