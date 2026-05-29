using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.Projects;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class DashboardDataLoader : IDashboardDataLoader
    {
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly IReadRepository<ProjectCost> projectCostRepository;
        private readonly IReadRepository<BaseCostAttachment> attachmentRepository;
        private readonly IReadRepository<CostEstimateItem> ceItemRepository;
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<ProjectCurrency> projectCurrencyRepository;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly IReadRepository<WorkScheduleStage> workScheduleStageRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IReadRepository<WorkScheduleStageWorkPeriod> stageWorkPeriodRepository;

        public DashboardDataLoader(
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<ProjectCost> projectCostRepository,
            IReadRepository<BaseCostAttachment> attachmentRepository,
            IReadRepository<CostEstimateItem> ceItemRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<ProjectCurrency> projectCurrencyRepository,
            IReadRepository<WorkSchedule> workScheduleRepository,
            IReadRepository<WorkScheduleStage> workScheduleStageRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            IReadRepository<WorkScheduleStageWorkPeriod> stageWorkPeriodRepository)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.projectCostRepository = projectCostRepository;
            this.attachmentRepository = attachmentRepository;
            this.ceItemRepository = ceItemRepository;
            this.costEstimateRepository = costEstimateRepository;
            this.projectCurrencyRepository = projectCurrencyRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.workScheduleStageRepository = workScheduleStageRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.stageWorkPeriodRepository = stageWorkPeriodRepository;
        }

        public async Task<DashboardData> LoadAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            List<BaseCost> allCosts = await LoadAllCostsAsync(tenantId, projectId);
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId = await LoadAttachmentLookupAsync(tenantId, projectId, allCosts);
            Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> costEstimateContext =
                await ResolveCostEstimateContextAsync(allCosts, cancellationToken);

            List<CostEstimate> allEstimates = (await costEstimateRepository.GetBySearch(
                ce => ce.ProjectId == projectId && ce.TenantId == tenantId)).ToList();

            ProjectCurrency? projectCurrency = await projectCurrencyRepository.GetFirstBySearch(
                c => c.ProjectId == projectId && c.Project.TenantId == tenantId,
                cancellationToken);

            List<WorkSchedule> allSchedules = (await workScheduleRepository.GetBySearch(
                ws => ws.ProjectId == projectId && ws.TenantId == tenantId)).ToList();

            HashSet<Guid> scheduleIds = allSchedules.Select(ws => ws.Id).ToHashSet();

            List<WorkScheduleStage> allStages = scheduleIds.Count > 0
                ? (await workScheduleStageRepository.GetBySearch(
                    s => s.TenantId == tenantId && s.ProjectId == projectId && scheduleIds.Contains(s.WorkScheduleId))).ToList()
                : new List<WorkScheduleStage>();

            HashSet<Guid> stageIds = allStages.Select(s => s.Id).ToHashSet();

            List<WorkScheduleStageWork> allStageWorks = stageIds.Count > 0
                ? (await stageWorkRepository.GetBySearch(
                    w => w.TenantId == tenantId && w.ProjectId == projectId && stageIds.Contains(w.WorkScheduleStageId))).ToList()
                : new List<WorkScheduleStageWork>();

            HashSet<Guid> workIds = allStageWorks.Select(w => w.Id).ToHashSet();
            HashSet<Guid> closedWorkIds = await ResolveClosedWorkIdsAsync(workIds);

            HashSet<Guid> linkedItemIds = allStageWorks
                .Where(w => w.CostEstimateItemId.HasValue)
                .Select(w => w.CostEstimateItemId!.Value)
                .ToHashSet();

            Dictionary<Guid, CostEstimateItem> stageWorkLinkedItems = linkedItemIds.Count > 0
                ? (await ceItemRepository.GetBySearch(i => linkedItemIds.Contains(i.Id))).ToDictionary(i => i.Id)
                : new Dictionary<Guid, CostEstimateItem>();

            return new DashboardData(
                AllCosts: allCosts,
                AttachmentsByCostId: attachmentsByCostId,
                CostEstimateContext: costEstimateContext,
                AllEstimates: allEstimates,
                ProjectCurrency: projectCurrency,
                AllSchedules: allSchedules,
                AllStages: allStages,
                AllStageWorks: allStageWorks,
                ClosedWorkIds: closedWorkIds,
                StageWorkLinkedItems: stageWorkLinkedItems);
        }

        private async Task<List<BaseCost>> LoadAllCostsAsync(Guid tenantId, Guid projectId)
        {
            List<TrackedCost> trackedCosts = (await trackedCostRepository.GetBySearch(
                tc => tc.TenantId == tenantId && tc.ProjectId == projectId))
                .ToList();

            List<ProjectCost> acceptedProjectCosts = (await projectCostRepository.GetBySearch(
                pc => pc.TenantId == tenantId && pc.ProjectId == projectId && pc.ApprovalStatus == CostApprovalStatus.Approved))
                .ToList();

            return trackedCosts.Cast<BaseCost>().Concat(acceptedProjectCosts).ToList();
        }

        private async Task<ILookup<Guid, BaseCostAttachment>> LoadAttachmentLookupAsync(
            Guid tenantId, Guid projectId, List<BaseCost> allCosts)
        {
            HashSet<Guid> costIds = allCosts.Select(c => c.Id).ToHashSet();

            List<BaseCostAttachment> allAttachments = costIds.Count > 0
                ? (await attachmentRepository.GetBySearch(
                    a => a.TenantId == tenantId && a.ProjectId == projectId && costIds.Contains(a.CostId))).ToList()
                : new List<BaseCostAttachment>();

            return allAttachments.ToLookup(a => a.CostId);
        }

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

        private async Task<HashSet<Guid>> ResolveClosedWorkIdsAsync(HashSet<Guid> workIds)
        {
            if (workIds.Count == 0)
            {
                return new HashSet<Guid>();
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
    }
}
