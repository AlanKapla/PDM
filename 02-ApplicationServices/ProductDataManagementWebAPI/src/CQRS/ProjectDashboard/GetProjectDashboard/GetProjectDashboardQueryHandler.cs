using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectDashboard.GetProjectDashboard
{
    public sealed class GetProjectDashboardQueryHandler
        : CostTrackerHandlerBase, IRequestHandler<GetProjectDashboardQuery, ProjectDashboardWeb>
    {
        private readonly IReadRepository<Project> projectRepository;
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly IReadRepository<ProjectCost> projectCostRepository;
        private readonly IReadRepository<BaseCostAttachment> attachmentRepository;
        private readonly IReadRepository<CostEstimateItem> ceItemRepository;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly IReadRepository<WorkScheduleStage> workScheduleStageRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IReadRepository<WorkScheduleStageWorkPeriod> stageWorkPeriodRepository;
        private readonly IReadRepository<ProjectCurrency> projectCurrencyRepository;
        private readonly ICostEstimateCacheService ceCacheService;

        public GetProjectDashboardQueryHandler(
            IReadRepository<Project> projectRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<ProjectCost> projectCostRepository,
            IReadRepository<BaseCostAttachment> attachmentRepository,
            IReadRepository<CostEstimateItem> ceItemRepository,
            IReadRepository<WorkSchedule> workScheduleRepository,
            IReadRepository<WorkScheduleStage> workScheduleStageRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            IReadRepository<WorkScheduleStageWorkPeriod> stageWorkPeriodRepository,
            IReadRepository<ProjectCurrency> projectCurrencyRepository,
            ICostEstimateCacheService ceCacheService,
            ICostTrackerFinancialService financialService,
            ICostTrackerTimelineService timelineService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser)
            : base(currentUser, trackedCostRepository, attachmentService, financialService, timelineService)
        {
            this.projectRepository = projectRepository;
            this.costEstimateRepository = costEstimateRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.projectCostRepository = projectCostRepository;
            this.attachmentRepository = attachmentRepository;
            this.ceItemRepository = ceItemRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.workScheduleStageRepository = workScheduleStageRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.stageWorkPeriodRepository = stageWorkPeriodRepository;
            this.projectCurrencyRepository = projectCurrencyRepository;
            this.ceCacheService = ceCacheService;
        }

        public async Task<ProjectDashboardWeb> Handle(
            GetProjectDashboardQuery request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

            Project project = await projectRepository.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            ProjectCurrency? projectCurrency = await projectCurrencyRepository.GetFirstBySearch(
                c => c.ProjectId == request.ProjectId,
                cancellationToken);

            List<BaseCost> allCosts = await LoadAllCostsAsync(request.TenantId, request.ProjectId);

            ILookup<Guid, BaseCostAttachment> attachmentsByCostId = await LoadAttachmentLookupAsync(allCosts);

            Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> costEstimateContext =
                await ResolveCostEstimateContextAsync(allCosts, cancellationToken);

            ProjectAdditionalCostsWeb projectAdditionalCosts = BuildProjectAdditionalCosts(
                allCosts, attachmentsByCostId, costEstimateContext);

            List<BaseCost> estimateScopedCosts = allCosts
                .Where(c => costEstimateContext.TryGetValue(c.Id, out var ctx) && ctx.CostEstimateId.HasValue)
                .ToList();

            List<CostEstimate> allEstimates = (await costEstimateRepository.GetBySearch(
                ce => ce.ProjectId == request.ProjectId && ce.TenantId == request.TenantId)).ToList();

            DateTime referenceDate = DateTime.UtcNow;
            DateTime generatedAt = DateTime.UtcNow;

            List<CostEstimateSummaryWeb> estimateSummaries = await BuildEstimateSummariesAsync(
                allEstimates, estimateScopedCosts, attachmentsByCostId, costEstimateContext,
                request.TenantId, request.ProjectId, referenceDate, cancellationToken);

            List<ScheduleSummaryWeb> scheduleSummaries = await BuildScheduleSummariesAsync(
                request.TenantId, request.ProjectId, allCosts, attachmentsByCostId, referenceDate);

            ProjectFinancialSummaryWeb financialSummary = BuildProjectFinancialSummary(
                estimateSummaries, projectAdditionalCosts, allCosts, project.BudgetNet, project.BudgetGross,
                workSchedulesCount: scheduleSummaries.Count);

            ProjectTimelineSummaryWeb timelineSummary = BuildProjectTimelineSummary(estimateSummaries, scheduleSummaries);

            Dictionary<Guid, (string ScheduleName, string StageName, string WorkItemName)> scheduleWorkItemContext =
                BuildScheduleWorkItemContext(scheduleSummaries);

            Dictionary<Guid, (string EstimateName, string GroupName, string ItemName)> estimateItemContext =
                BuildEstimateItemContext(estimateSummaries);

            List<TrackedCostWeb> allCostWebs = BuildAllCosts(allCosts, attachmentsByCostId, scheduleWorkItemContext, estimateItemContext);

            return new ProjectDashboardWeb
            {
                ProjectId = request.ProjectId,
                GeneratedAt = generatedAt,
                ReferenceDate = referenceDate,
                SelectedCurrencyCode = projectCurrency?.Code,
                SelectedCurrencySymbol = projectCurrency?.Symbol,
                FinancialSummary = financialSummary,
                TimelineSummary = timelineSummary,
                CostEstimateSummaries = estimateSummaries,
                ScheduleSummaries = scheduleSummaries,
                ProjectAdditionalCosts = projectAdditionalCosts,
                AllCosts = allCostWebs
            };
        }

        private async Task<List<BaseCost>> LoadAllCostsAsync(Guid tenantId, Guid projectId)
        {
            List<TrackedCost> trackedCosts = (await trackedCostRepository.GetBySearch(
                tc => tc.TenantId == tenantId && tc.ProjectId == projectId))
                .ToList();

            List<ProjectCost> acceptedProjectCosts = (await projectCostRepository.GetBySearch(
                pc => pc.TenantId == tenantId && pc.ProjectId == projectId && pc.IsAccepted))
                .ToList();

            return trackedCosts.Cast<BaseCost>().Concat(acceptedProjectCosts).ToList();
        }

        private async Task<ILookup<Guid, BaseCostAttachment>> LoadAttachmentLookupAsync(List<BaseCost> allCosts)
        {
            HashSet<Guid> costIds = allCosts.Select(c => c.Id).ToHashSet();

            List<BaseCostAttachment> allAttachments = costIds.Count > 0
                ? (await attachmentRepository.GetBySearch(a => costIds.Contains(a.CostId))).ToList()
                : new List<BaseCostAttachment>();

            return allAttachments.ToLookup(a => a.CostId);
        }

        /// <summary>
        /// Batch-resolves (CostEstimateId, CostEstimateItemId) per cost via direct CostEstimateItemId FK.
        /// </summary>
        private async Task<Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)>> ResolveCostEstimateContextAsync(
            List<BaseCost> costs,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> directCeItemIds = costs
                .Where(c => c.CostEstimateItemId.HasValue)
                .Select(c => c.CostEstimateItemId!.Value)
                .ToHashSet();

            Dictionary<Guid, CostEstimateItem> ceItemsById = directCeItemIds.Count > 0
                ? (await ceItemRepository.GetBySearch(i => directCeItemIds.Contains(i.Id)))
                    .ToDictionary(i => i.Id)
                : new Dictionary<Guid, CostEstimateItem>();

            Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> result =
                new Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)>();

            foreach (BaseCost cost in costs)
            {
                Guid? ceItemId = null;
                Guid? ceId = null;

                if (cost.CostEstimateItemId.HasValue &&
                    ceItemsById.TryGetValue(cost.CostEstimateItemId.Value, out CostEstimateItem? item))
                {
                    ceItemId = cost.CostEstimateItemId;
                    ceId = item.CostEstimateId;
                }

                result[cost.Id] = (ceId, ceItemId);
            }

            return result;
        }

        private ProjectAdditionalCostsWeb BuildProjectAdditionalCosts(
            List<BaseCost> allCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> costEstimateContext)
        {
            List<TrackedCost> additionalTrackedCosts = allCosts.OfType<TrackedCost>()
                .Where(tc => !tc.CostEstimateItemId.HasValue && !tc.WorkScheduleStageWorkId.HasValue)
                .ToList();

            List<TrackedCostWeb> trackedCostWebs = additionalTrackedCosts
                .Select(c => MapTrackedCostToWeb(c, attachmentsByCostId[c.Id]))
                .ToList();

            List<TrackedCostWeb> projectCostWebs = allCosts.OfType<ProjectCost>()
                .Select(c => MapProjectCostToWeb(c, attachmentsByCostId[c.Id]))
                .ToList();

            List<TrackedCostWeb> allAdditionalCosts = trackedCostWebs.Concat(projectCostWebs).ToList();

            decimal? totalNet = allAdditionalCosts.Any(c => c.Net.HasValue)
                ? allAdditionalCosts.Sum(c => c.Net ?? 0)
                : null;

            decimal? totalGross = allAdditionalCosts.Any(c => c.Gross.HasValue)
                ? allAdditionalCosts.Sum(c => c.Gross ?? 0)
                : null;

            return new ProjectAdditionalCostsWeb
            {
                TotalNet = totalNet,
                TotalGross = totalGross,
                CostsCount = allAdditionalCosts.Count,
                Costs = allAdditionalCosts
            };
        }

        private async Task<List<CostEstimateSummaryWeb>> BuildEstimateSummariesAsync(
            List<CostEstimate> allEstimates,
            List<BaseCost> estimateScopedCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> costEstimateContext,
            Guid tenantId,
            Guid projectId,
            DateTime referenceDate,
            CancellationToken cancellationToken)
        {
            List<CostEstimateSummaryWeb> summaries = new List<CostEstimateSummaryWeb>();

            List<WorkScheduleStageWork> linkedStageWorks = (await stageWorkRepository.GetBySearch(
                w => w.TenantId == tenantId && w.ProjectId == projectId && w.CostEstimateItemId.HasValue)).ToList();

            ILookup<Guid, WorkScheduleStageWork> stageWorksByItemId =
                linkedStageWorks.ToLookup(w => w.CostEstimateItemId!.Value);

            HashSet<Guid> linkedWorkIds = linkedStageWorks.Select(w => w.Id).ToHashSet();
            HashSet<Guid> closedWorkIds = await ResolveClosedWorkIdsAsync(linkedWorkIds);

            List<WorkSchedule> linkedSchedules = (await workScheduleRepository.GetBySearch(
                ws => ws.CostEstimateId.HasValue && ws.ProjectId == projectId)).ToList();
            Dictionary<Guid, Guid> workScheduleIdByEstimateId = linkedSchedules
                .GroupBy(ws => ws.CostEstimateId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Id);

            foreach (CostEstimate costEstimate in allEstimates)
            {
                Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);
                Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);

                List<BaseCost> estimateCosts = estimateScopedCosts
                    .Where(c => costEstimateContext.TryGetValue(c.Id, out var ctx) && ctx.CostEstimateId == costEstimate.Id)
                    .ToList();

                ILookup<Guid, TrackedCost> costsByItemId = estimateCosts.OfType<TrackedCost>()
                    .Where(tc => costEstimateContext.TryGetValue(tc.Id, out var ctx) && ctx.CostEstimateItemId.HasValue)
                    .ToLookup(tc => costEstimateContext[tc.Id].CostEstimateItemId!.Value);

                List<TrackedCost> additionalCostsList = estimateCosts.OfType<TrackedCost>()
                    .Where(tc => !costEstimateContext.TryGetValue(tc.Id, out var ctx) || !ctx.CostEstimateItemId.HasValue)
                    .ToList();

                List<TrackerGroupWeb> groups = BuildTrackerGroups(
                    groupsDict, itemsDict,
                    costsByItemId, attachmentsByCostId, stageWorksByItemId, closedWorkIds, referenceDate);

                List<TrackedCostWeb> additionalCostWebs = additionalCostsList
                    .Select(tc => MapTrackedCostToWeb(tc, attachmentsByCostId[tc.Id]))
                    .ToList();

                Guid? linkedWorkScheduleId = workScheduleIdByEstimateId.TryGetValue(costEstimate.Id, out Guid wsId) ? wsId : null;

                summaries.Add(BuildEstimateSummary(
                    costEstimate, itemsDict, costsByItemId, additionalCostsList,
                    groups, additionalCostWebs, referenceDate, linkedWorkScheduleId));
            }

            return summaries;
        }

        private async Task<List<ScheduleSummaryWeb>> BuildScheduleSummariesAsync(
            Guid tenantId,
            Guid projectId,
            List<BaseCost> allCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            DateTime referenceDate)
        {
            List<WorkSchedule> schedules = (await workScheduleRepository.GetBySearch(
                ws => ws.ProjectId == projectId && ws.TenantId == tenantId)).ToList();

            if (schedules.Count == 0) return [];

            HashSet<Guid> scheduleIds = schedules.Select(ws => ws.Id).ToHashSet();

            List<WorkScheduleStage> allStages = (await workScheduleStageRepository.GetBySearch(
                s => s.ProjectId == projectId && scheduleIds.Contains(s.WorkScheduleId))).ToList();

            ILookup<Guid, WorkScheduleStage> stagesByScheduleId = allStages.ToLookup(s => s.WorkScheduleId);

            HashSet<Guid> stageIds = allStages.Select(s => s.Id).ToHashSet();

            List<WorkScheduleStageWork> allStageWorks = stageIds.Count > 0
                ? (await stageWorkRepository.GetBySearch(w => stageIds.Contains(w.WorkScheduleStageId))).ToList()
                : [];

            ILookup<Guid, WorkScheduleStageWork> worksByStageId = allStageWorks.ToLookup(w => w.WorkScheduleStageId);

            HashSet<Guid> workIds = allStageWorks.Select(w => w.Id).ToHashSet();
            HashSet<Guid> closedWorkIds = await ResolveClosedWorkIdsAsync(workIds);

            ILookup<Guid, TrackedCost> costsByStageWorkId = allCosts.OfType<TrackedCost>()
                .Where(tc => tc.WorkScheduleStageWorkId.HasValue && workIds.Contains(tc.WorkScheduleStageWorkId!.Value))
                .ToLookup(tc => tc.WorkScheduleStageWorkId!.Value);

            HashSet<Guid> ceItemIds = allStageWorks
                .Where(w => w.CostEstimateItemId.HasValue)
                .Select(w => w.CostEstimateItemId!.Value)
                .ToHashSet();

            Dictionary<Guid, CostEstimateItem> ceItemsById = ceItemIds.Count > 0
                ? (await ceItemRepository.GetBySearch(i => ceItemIds.Contains(i.Id)))
                    .ToDictionary(i => i.Id)
                : new Dictionary<Guid, CostEstimateItem>();

            return schedules
                .Select(schedule => BuildScheduleSummaryWeb(
                    schedule,
                    stagesByScheduleId[schedule.Id].OrderBy(s => s.Order).ToList(),
                    worksByStageId,
                    costsByStageWorkId,
                    attachmentsByCostId,
                    closedWorkIds,
                    ceItemsById,
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
                .Where(s => s.ParentStageId == null)
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
            bool hasLinkedSchedule = scheduleTimeline != null;

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
                WorkItemsWithCostsCount  = stageWebs.Sum(s => CountWorkItemsWithCosts(s)),
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
                : [];

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
            bool hasLinkedSchedule = stageTimeline != null;

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

        private async Task<HashSet<Guid>> ResolveClosedWorkIdsAsync(HashSet<Guid> workIds)
        {
            if (workIds.Count == 0)
            {
                return [];
            }

            List<WorkScheduleStageWorkPeriod> periods = (await stageWorkPeriodRepository.GetBySearch(
                p => workIds.Contains(p.WorkScheduleStageWorkId))).ToList();

            ILookup<Guid, WorkScheduleStageWorkPeriod> periodsByWorkId = periods.ToLookup(p => p.WorkScheduleStageWorkId);

            return workIds
                .Where(id =>
                {
                    IEnumerable<WorkScheduleStageWorkPeriod> workPeriods = periodsByWorkId[id];
                    return workPeriods.Any() && workPeriods.All(p => p.IsClosed);
                })
                .ToHashSet();
        }

        private List<TrackedCostWeb> BuildAllCosts(
            List<BaseCost> allCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            Dictionary<Guid, (string ScheduleName, string StageName, string WorkItemName)> scheduleWorkItemContext,
            Dictionary<Guid, (string EstimateName, string GroupName, string ItemName)> estimateItemContext)
        {
            List<TrackedCostWeb> trackedCostWebs = allCosts.OfType<TrackedCost>().Select(tc =>
            {
                TrackedCostWeb web = MapTrackedCostToWeb(tc, attachmentsByCostId[tc.Id]);

                scheduleWorkItemContext.TryGetValue(tc.Id, out (string ScheduleName, string StageName, string WorkItemName) schedCtx);
                estimateItemContext.TryGetValue(tc.Id, out (string EstimateName, string GroupName, string ItemName) estCtx);

                return web with
                {
                    ScheduleName = schedCtx.ScheduleName,
                    StageName = schedCtx.StageName,
                    WorkItemName = schedCtx.WorkItemName,
                    EstimateName = estCtx.EstimateName,
                    EstimateGroupName = estCtx.GroupName,
                    EstimateItemName = estCtx.ItemName
                };
            }).ToList();

            List<TrackedCostWeb> projectCostWebs = allCosts.OfType<ProjectCost>()
                .Select(pc => MapProjectCostToWeb(pc, attachmentsByCostId[pc.Id]))
                .ToList();

            return trackedCostWebs.Concat(projectCostWebs).ToList();
        }

        private static Dictionary<Guid, (string EstimateName, string GroupName, string ItemName)> BuildEstimateItemContext(
            List<CostEstimateSummaryWeb> estimateSummaries)
        {
            Dictionary<Guid, (string, string, string)> result = new Dictionary<Guid, (string, string, string)>();

            foreach (CostEstimateSummaryWeb estimate in estimateSummaries)
            {
                foreach (TrackerGroupWeb group in FlattenGroups(estimate.Groups))
                {
                    foreach (WorkItemLinkWeb workItem in group.Items)
                    {
                        foreach (TrackedCostWeb cost in workItem.Costs)
                        {
                            result[cost.Id] = (estimate.CostEstimateName, group.GroupName, workItem.DisplayName);
                        }
                    }
                }
            }

            return result;
        }

        private static IEnumerable<TrackerGroupWeb> FlattenGroups(IEnumerable<TrackerGroupWeb> groups)
        {
            foreach (TrackerGroupWeb group in groups)
            {
                yield return group;

                foreach (TrackerGroupWeb child in FlattenGroups(group.ChildGroups))
                {
                    yield return child;
                }
            }
        }

        private static Dictionary<Guid, (string ScheduleName, string StageName, string WorkItemName)> BuildScheduleWorkItemContext(
            List<ScheduleSummaryWeb> scheduleSummaries)
        {
            Dictionary<Guid, (string, string, string)> result = new Dictionary<Guid, (string, string, string)>();

            foreach (ScheduleSummaryWeb schedule in scheduleSummaries)
            {
                foreach (ScheduleStageWeb stage in FlattenStages(schedule.Stages))
                {
                    foreach (WorkItemLinkWeb workItem in stage.WorkItems)
                    {
                        foreach (TrackedCostWeb cost in workItem.Costs)
                        {
                            result[cost.Id] = (schedule.WorkScheduleName, stage.StageName, workItem.DisplayName);
                        }
                    }
                }
            }

            return result;
        }

        private static IEnumerable<ScheduleStageWeb> FlattenStages(IEnumerable<ScheduleStageWeb> stages)
        {
            foreach (ScheduleStageWeb stage in stages)
            {
                yield return stage;

                foreach (ScheduleStageWeb child in FlattenStages(stage.ChildStages))
                {
                    yield return child;
                }
            }
        }

        private static ScheduleCostSummaryWeb BuildScheduleCostSummary(List<ScheduleSummaryWeb> scheduleSummaries)
        {
            decimal totalCostsNet = scheduleSummaries.Sum(s => s.TotalWorkItemsCostsNet ?? 0);
            decimal totalCostsGross = scheduleSummaries.Sum(s => s.TotalWorkItemsCostsGross ?? 0);
            int withCosts = scheduleSummaries.Count(s => s.TotalWorkItemsCostsNet.HasValue && s.TotalWorkItemsCostsNet.Value > 0);

            return new ScheduleCostSummaryWeb
            {
                TotalSchedulesCostsNet = totalCostsNet,
                TotalSchedulesCostsGross = totalCostsGross,
                SchedulesWithCostsCount = withCosts,
                SchedulesWithoutCostsCount = scheduleSummaries.Count - withCosts
            };
        }

        private static decimal? CombineNullableStage(decimal? a, decimal? b)
        {
            if (!a.HasValue && !b.HasValue) return null;
            return (a ?? 0) + (b ?? 0);
        }

        private static ProjectTimelineSummaryWeb BuildProjectTimelineSummary(
            IReadOnlyCollection<CostEstimateSummaryWeb> estimateSummaries,
            IReadOnlyCollection<ScheduleSummaryWeb> scheduleSummaries)
        {
            List<TimelineStatsWeb> stats = scheduleSummaries
                .Where(s => s.Timeline != null)
                .Select(s => s.Timeline!)
                .ToList();

            int totalWorkCount     = stats.Sum(s => s.TotalWorkCount);
            int completedCount     = stats.Sum(s => s.CompletedCount);
            int completedLateCount = stats.Sum(s => s.CompletedLateCount);
            int inProgressCount    = stats.Sum(s => s.InProgressCount);
            int notStartedCount    = stats.Sum(s => s.NotStartedCount);
            int delayedCount       = stats.Sum(s => s.DelayedCount);

            TimelineStatus overallStatus;
            if (scheduleSummaries.Count == 0)
            {
                overallStatus = TimelineStatus.NoSchedule;
            }
            else if (scheduleSummaries.Sum(s => s.TotalWorkItemsCount) == 0)
            {
                overallStatus = TimelineStatus.NotConfigured;
            }
            else if (stats.Count == 0)
            {
                overallStatus = scheduleSummaries.Any(s => s.TimelineStatus == TimelineStatus.NotConfigured)
                    ? TimelineStatus.NotConfigured
                    : TimelineStatus.NoSchedule;
            }
            else if (stats.All(s => s.OverallStatus is TimelineStatus.NotConfigured or TimelineStatus.NoSchedule))
            {
                overallStatus = stats.Any(s => s.OverallStatus == TimelineStatus.NotConfigured)
                    ? TimelineStatus.NotConfigured
                    : TimelineStatus.NoSchedule;
            }
            else
            {
                overallStatus = stats.Select(s => s.OverallStatus).MaxBy(s => s switch
                {
                    TimelineStatus.Delayed       => 5,
                    TimelineStatus.CompletedLate => 4,
                    TimelineStatus.InProgress    => 3,
                    TimelineStatus.NotStarted    => 2,
                    TimelineStatus.Completed     => 1,
                    _                            => 0
                });
            }

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
    }
}
