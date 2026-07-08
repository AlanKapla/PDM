using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using Business.Interfaces.WebModels.ProjectDashboard;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.Projects;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class ProjectDashboardAssembler : CostTrackerHandlerBase, IProjectDashboardAssembler
    {
        private readonly IScheduleSummaryBuilder scheduleSummaryBuilder;
        private readonly IProjectTimelineAggregator timelineAggregator;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly IReadRepository<ProjectCostCategory> categoryRepo;

        public ProjectDashboardAssembler(
            ICurrentUser currentUser,
            IRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService,
            ICostTrackerFinancialService financialService,
            ICostTrackerTimelineService timelineService,
            IContractorService contractorService,
            IScheduleSummaryBuilder scheduleSummaryBuilder,
            IProjectTimelineAggregator timelineAggregator,
            ICostEstimateCacheService ceCacheService,
            IReadRepository<ProjectCostCategory> categoryRepo)
            : base(currentUser, trackedCostRepository, attachmentService, financialService, timelineService, contractorService)
        {
            this.scheduleSummaryBuilder = scheduleSummaryBuilder;
            this.timelineAggregator = timelineAggregator;
            this.ceCacheService = ceCacheService;
            this.categoryRepo = categoryRepo;
        }

        public async Task<ProjectDashboardWeb> AssembleAsync(
            Project project,
            DashboardData data,
            CancellationToken cancellationToken)
        {
            DateTime referenceDate = DateTime.UtcNow;
            DateTime generatedAt = DateTime.UtcNow;

            Dictionary<Guid, string> contractorNames = await LoadContractorNamesAsync(
                data.AllCosts, project.TenantId, cancellationToken);

            await LoadCategoryInfoAsync(
                data.AllCosts, project.Id, categoryRepo, cancellationToken);

            // Jednorazowe mapowanie każdego kosztu (TrackedCost i ProjectCost) -> TrackedCostWeb, reużywane wszędzie poniżej.
            // Klasyfikacja (IsAdditional / SourceType) wynika z powiązań kosztu, niezależnie od typu encji.
            Dictionary<Guid, TrackedCostWeb> costWebsById = data.AllCosts
                .ToDictionary(c => c.Id, c => MapCostToWeb(c, data.AttachmentsByCostId[c.Id]));

            ProjectAdditionalCostsWeb projectAdditionalCosts = BuildProjectAdditionalCosts(
                data.AllCosts, costWebsById);

            List<BaseCost> estimateScopedCosts = data.AllCosts
                .Where(c => data.CostEstimateContext.TryGetValue(c.Id, out (Guid? CostEstimateId, Guid? CostEstimateItemId) ctx) && ctx.CostEstimateId.HasValue)
                .ToList();

            List<CostEstimateSummaryWeb> estimateSummaries = await BuildEstimateSummariesAsync(
                data, estimateScopedCosts, project.TenantId, project.Id, referenceDate, cancellationToken);

            List<ScheduleSummaryWeb> scheduleSummaries = scheduleSummaryBuilder.BuildAll(
                data.AllSchedules, data.AllStages, data.AllStageWorks, data.ClosedWorkIds,
                data.StageWorkLinkedItems, data.AllCosts, data.AttachmentsByCostId, referenceDate, contractorNames);

            ProjectFinancialSummaryWeb financialSummary = BuildProjectFinancialSummary(
                estimateSummaries, projectAdditionalCosts, data.AllCosts,
                project.BudgetNet, project.BudgetGross, workSchedulesCount: scheduleSummaries.Count);

            ProjectTimelineSummaryWeb timelineSummary = timelineAggregator.Build(estimateSummaries, scheduleSummaries);

            Dictionary<Guid, (string ScheduleName, string StageName, string WorkItemName)> scheduleWorkItemContext =
                BuildScheduleWorkItemContext(scheduleSummaries);

            Dictionary<Guid, (string EstimateName, string GroupName, string ItemName)> estimateItemContext =
                BuildEstimateItemContext(estimateSummaries);

            Dictionary<Guid, string> estimateItemPathContext =
                BuildEstimateItemPathContext(estimateSummaries);

            Dictionary<Guid, string> scheduleWorkPathContext =
                BuildScheduleWorkPathContext(scheduleSummaries);

            List<TrackedCostWeb> allCostWebs = BuildAllCosts(
                data.AllCosts, costWebsById,
                scheduleWorkItemContext, estimateItemContext,
                estimateItemPathContext, scheduleWorkPathContext);

            List<CostByCategoryWeb> costByCategory = await BuildCostByCategoryAsync(
                data.AllCosts, project.Id, cancellationToken);

            return new ProjectDashboardWeb
            {
                ProjectId = project.Id,
                GeneratedAt = generatedAt,
                ReferenceDate = referenceDate,
                SelectedCurrencyCode = data.ProjectCurrency?.Code,
                SelectedCurrencySymbol = data.ProjectCurrency?.Symbol,
                FinancialSummary = financialSummary,
                TimelineSummary = timelineSummary,
                CostEstimateSummaries = estimateSummaries,
                ScheduleSummaries = scheduleSummaries,
                ProjectAdditionalCosts = projectAdditionalCosts,
                AllCosts = allCostWebs,
                CostByCategory = costByCategory
            };
        }

        private async Task<List<CostByCategoryWeb>> BuildCostByCategoryAsync(
            List<BaseCost> allCosts,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            const string uncategorizedLabel = "Bez kategorii";

            IEnumerable<ProjectCostCategory> projectCategories = await categoryRepo.GetBySearch(
                c => c.ProjectId == projectId);

            Dictionary<Guid, ProjectCostCategory> categoryDict = projectCategories.ToDictionary(c => c.Id);

            IEnumerable<IGrouping<Guid?, BaseCost>> groups = allCosts.GroupBy(c => c.CategoryId);

            List<CostByCategoryWeb> result = new List<CostByCategoryWeb>();

            foreach (IGrouping<Guid?, BaseCost> group in groups)
            {
                decimal net = group.Sum(c => c.Net ?? 0);
                decimal? gross = group.Any(c => c.Gross.HasValue)
                    ? group.Sum(c => c.Gross ?? 0)
                    : null;

                if (group.Key is null)
                {
                    result.Add(new CostByCategoryWeb
                    {
                        CategoryId = null,
                        CategoryName = uncategorizedLabel,
                        Color = null,
                        Net = net,
                        Gross = gross,
                        CostsCount = group.Count()
                    });
                    continue;
                }

                ProjectCostCategory? category = categoryDict.GetValueOrDefault(group.Key.Value);
                string categoryName = category?.Name ?? uncategorizedLabel;

                result.Add(new CostByCategoryWeb
                {
                    CategoryId = group.Key,
                    CategoryName = categoryName,
                    Color = category?.Color,
                    Net = net,
                    Gross = gross,
                    CostsCount = group.Count()
                });
            }

            return result.OrderByDescending(c => c.Net).ToList();
        }

        private static ProjectAdditionalCostsWeb BuildProjectAdditionalCosts(
            List<BaseCost> allCosts,
            Dictionary<Guid, TrackedCostWeb> costWebsById)
        {
            // Koszt dodatkowy = niepowiązany z kosztorysem ani harmonogramem, niezależnie czy to TrackedCost czy ProjectCost.
            List<TrackedCostWeb> all = allCosts
                .Where(c => !c.CostEstimateItemId.HasValue && !c.WorkScheduleStageWorkId.HasValue)
                .Select(c => costWebsById[c.Id])
                .ToList();

            decimal? totalNet = all.Any(c => c.Net.HasValue) ? all.Sum(c => c.Net ?? 0) : null;
            decimal? totalGross = all.Any(c => c.Gross.HasValue) ? all.Sum(c => c.Gross ?? 0) : null;

            return new ProjectAdditionalCostsWeb
            {
                TotalNet = totalNet,
                TotalGross = totalGross,
                CostsCount = all.Count,
                Costs = all
            };
        }

        private async Task<List<CostEstimateSummaryWeb>> BuildEstimateSummariesAsync(
            DashboardData data,
            List<BaseCost> estimateScopedCosts,
            Guid tenantId,
            Guid projectId,
            DateTime referenceDate,
            CancellationToken cancellationToken)
        {
            List<CostEstimateSummaryWeb> summaries = new List<CostEstimateSummaryWeb>();

            ILookup<Guid, Entities.Models.WorkSchedules.WorkScheduleStageWork> stageWorksByItemId =
                data.AllStageWorks
                    .Where(w => w.CostEstimateItemId.HasValue)
                    .ToLookup(w => w.CostEstimateItemId!.Value);

            HashSet<Guid> linkedWorkIds = data.AllStageWorks
                .Where(w => w.CostEstimateItemId.HasValue)
                .Select(w => w.Id)
                .ToHashSet();

            HashSet<Guid> closedLinkedWorkIds = data.ClosedWorkIds
                .Where(linkedWorkIds.Contains)
                .ToHashSet();

            Dictionary<Guid, Guid> workScheduleIdByEstimateId = data.AllSchedules
                .Where(ws => ws.CostEstimateId.HasValue)
                .GroupBy(ws => ws.CostEstimateId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Id);

            foreach (CostEstimate costEstimate in data.AllEstimates)
            {
                Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);
                Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);

                List<BaseCost> estimateCosts = estimateScopedCosts
                    .Where(c => data.CostEstimateContext.TryGetValue(c.Id, out (Guid? CostEstimateId, Guid? CostEstimateItemId) ctx) && ctx.CostEstimateId == costEstimate.Id)
                    .ToList();

                ILookup<Guid, BaseCost> costsByItemId = estimateCosts
                    .Where(c => data.CostEstimateContext.TryGetValue(c.Id, out (Guid? CostEstimateId, Guid? CostEstimateItemId) ctx) && ctx.CostEstimateItemId.HasValue)
                    .ToLookup(c => data.CostEstimateContext[c.Id].CostEstimateItemId!.Value);

                List<BaseCost> additionalCostsList = estimateCosts
                    .Where(c => !data.CostEstimateContext.TryGetValue(c.Id, out (Guid? CostEstimateId, Guid? CostEstimateItemId) ctx) || !ctx.CostEstimateItemId.HasValue)
                    .ToList();

                List<TrackerGroupWeb> groups = BuildTrackerGroups(
                    groupsDict, itemsDict,
                    costsByItemId, data.AttachmentsByCostId, stageWorksByItemId, closedLinkedWorkIds, referenceDate);

                List<TrackedCostWeb> additionalCostWebs = additionalCostsList
                    .Select(c => MapCostToWeb(c, data.AttachmentsByCostId[c.Id]))
                    .ToList();

                Guid? linkedWorkScheduleId = workScheduleIdByEstimateId.TryGetValue(costEstimate.Id, out Guid wsId) ? wsId : null;

                summaries.Add(BuildEstimateSummary(
                    costEstimate, itemsDict, costsByItemId, additionalCostsList,
                    groups, additionalCostWebs, referenceDate, linkedWorkScheduleId));
            }

            return summaries;
        }

        private static List<TrackedCostWeb> BuildAllCosts(
            List<BaseCost> allCosts,
            Dictionary<Guid, TrackedCostWeb> costWebsById,
            Dictionary<Guid, (string ScheduleName, string StageName, string WorkItemName)> scheduleWorkItemContext,
            Dictionary<Guid, (string EstimateName, string GroupName, string ItemName)> estimateItemContext,
            Dictionary<Guid, string> estimateItemPathContext,
            Dictionary<Guid, string> scheduleWorkPathContext)
        {
            // Wzbogacamy każdy koszt (TrackedCost i ProjectCost) o kontekst kosztorysu/harmonogramu tak samo.
            return allCosts.Select(c =>
            {
                TrackedCostWeb web = costWebsById[c.Id];

                scheduleWorkItemContext.TryGetValue(c.Id, out (string ScheduleName, string StageName, string WorkItemName) schedCtx);
                estimateItemContext.TryGetValue(c.Id, out (string EstimateName, string GroupName, string ItemName) estCtx);
                estimateItemPathContext.TryGetValue(c.Id, out string? estimatePath);
                scheduleWorkPathContext.TryGetValue(c.Id, out string? workPath);

                return web with
                {
                    ScheduleName = schedCtx.ScheduleName,
                    StageName = schedCtx.StageName,
                    WorkItemName = schedCtx.WorkItemName,
                    EstimateName = estCtx.EstimateName,
                    EstimateGroupName = estCtx.GroupName,
                    EstimateItemName = estCtx.ItemName,
                    CostEstimateItemPath = estimatePath,
                    WorkScheduleWorkPath = workPath
                };
            }).ToList();
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

        private static Dictionary<Guid, string> BuildEstimateItemPathContext(
            List<CostEstimateSummaryWeb> estimateSummaries)
        {
            Dictionary<Guid, string> result = new Dictionary<Guid, string>();

            foreach (CostEstimateSummaryWeb estimate in estimateSummaries)
            {
                foreach ((TrackerGroupWeb group, string groupPath) in FlattenGroupsWithPath(estimate.Groups, string.Empty))
                {
                    foreach (WorkItemLinkWeb workItem in group.Items)
                    {
                        foreach (TrackedCostWeb cost in workItem.Costs)
                        {
                            result[cost.Id] = $"{estimate.CostEstimateName} > {groupPath} > {workItem.DisplayName}";
                        }
                    }
                }
            }

            return result;
        }

        private static Dictionary<Guid, string> BuildScheduleWorkPathContext(
            List<ScheduleSummaryWeb> scheduleSummaries)
        {
            Dictionary<Guid, string> result = new Dictionary<Guid, string>();

            foreach (ScheduleSummaryWeb schedule in scheduleSummaries)
            {
                foreach ((ScheduleStageWeb stage, string stagePath) in FlattenStagesWithPath(schedule.Stages, string.Empty))
                {
                    foreach (WorkItemLinkWeb workItem in stage.WorkItems)
                    {
                        foreach (TrackedCostWeb cost in workItem.Costs)
                        {
                            result[cost.Id] = $"{schedule.WorkScheduleName} > {stagePath} > {workItem.DisplayName}";
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

        private static IEnumerable<(TrackerGroupWeb Group, string FullPath)> FlattenGroupsWithPath(
            IEnumerable<TrackerGroupWeb> groups, string parentPath)
        {
            foreach (TrackerGroupWeb group in groups)
            {
                string path = string.IsNullOrEmpty(parentPath) ? group.GroupName : $"{parentPath} > {group.GroupName}";
                yield return (group, path);

                foreach ((TrackerGroupWeb child, string childPath) in FlattenGroupsWithPath(group.ChildGroups, path))
                {
                    yield return (child, childPath);
                }
            }
        }

        private static IEnumerable<(ScheduleStageWeb Stage, string FullPath)> FlattenStagesWithPath(
            IEnumerable<ScheduleStageWeb> stages, string parentPath)
        {
            foreach (ScheduleStageWeb stage in stages)
            {
                string path = string.IsNullOrEmpty(parentPath) ? stage.StageName : $"{parentPath} > {stage.StageName}";
                yield return (stage, path);

                foreach ((ScheduleStageWeb child, string childPath) in FlattenStagesWithPath(stage.ChildStages, path))
                {
                    yield return (child, childPath);
                }
            }
        }
    }
}
