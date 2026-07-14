using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class WorkScheduleSyncService : IWorkScheduleSyncService
    {
        private readonly IRepository<CostEstimateGroup> costEstimateGroupRepo;
        private readonly IRepository<CostEstimateItem> costEstimateItemRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IRepository<TrackedCost> trackedCostRepo;
        private readonly ILogger<WorkScheduleSyncService> logger;

        private const string DefaultWorkColorRgb = "#3B82F6";

        public WorkScheduleSyncService(
            IRepository<CostEstimateGroup> costEstimateGroupRepo,
            IRepository<CostEstimateItem> costEstimateItemRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IRepository<TrackedCost> trackedCostRepo,
            ILogger<WorkScheduleSyncService> logger)
        {
            this.costEstimateGroupRepo = costEstimateGroupRepo;
            this.costEstimateItemRepo = costEstimateItemRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.dependencyRepo = dependencyRepo;
            this.trackedCostRepo = trackedCostRepo;
            this.logger = logger;
        }

        public async Task<List<WorkScheduleStage>> SyncFromCostEstimateAsync(
            WorkSchedule workSchedule,
            CancellationToken cancellationToken)
        {
            if (!workSchedule.CostEstimateId.HasValue)
            {
                throw new InvalidOperationException(
                    $"WorkSchedule {workSchedule.Id} is not linked to a cost estimate.");
            }

            Guid costEstimateId = workSchedule.CostEstimateId.Value;

            List<CostEstimateGroup> allGroups = (await costEstimateGroupRepo.GetBySearch(
                g => g.CostEstimateId == costEstimateId && !g.IsDeleted))
                .ToList();

            List<WorkScheduleStage> allStages = (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workSchedule.Id && !s.IsDeleted))
                .ToList();

            HashSet<Guid> activeGroupIds = allGroups.Select(g => g.Id).ToHashSet();
            HashSet<Guid> softDeletedStageIds = await SoftDeleteObsoleteStagesAsync(
                allStages, activeGroupIds, workSchedule, cancellationToken);

            Dictionary<Guid, WorkScheduleStage> existingStagesByGroupId = allStages
                .Where(s => s.CostEstimateGroupId.HasValue && !softDeletedStageIds.Contains(s.Id))
                .ToDictionary(s => s.CostEstimateGroupId!.Value);

            (List<CostEstimateGroup> rootGroups, Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParent) =
                BuildGroupHierarchy(allGroups);

            List<WorkScheduleStage> resultStages = new List<WorkScheduleStage>();

            await ProcessGroupsAsync(
                rootGroups, null, workSchedule,
                existingStagesByGroupId, childGroupsByParent,
                resultStages, cancellationToken);

            await stageRepo.SaveChangesAsync(cancellationToken);

            Dictionary<Guid, WorkScheduleStage> stageByGroupId = resultStages
                .Where(s => s.CostEstimateGroupId.HasValue)
                .ToDictionary(s => s.CostEstimateGroupId!.Value);

            await SyncWorksFromItemsAsync(workSchedule, stageByGroupId, costEstimateId, cancellationToken);
            await workRepo.SaveChangesAsync(cancellationToken);

            return resultStages;
        }

        private async Task<HashSet<Guid>> SoftDeleteObsoleteStagesAsync(
            List<WorkScheduleStage> allStages,
            HashSet<Guid> activeGroupIds,
            WorkSchedule workSchedule,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStage> stagesToSoftDelete = allStages
                .Where(s => s.CostEstimateGroupId.HasValue && !activeGroupIds.Contains(s.CostEstimateGroupId!.Value))
                .ToList();

            DateTime now = DateTime.UtcNow;
            foreach (WorkScheduleStage stage in stagesToSoftDelete)
            {
                stage.IsDeleted = true;
                stage.DeletedAt = now;
                await stageRepo.Update(stage);
                logger.LogInformation(
                    "Soft-deleted work schedule stage {StageId} — linked cost estimate group {GroupId} is no longer active",
                    stage.Id, stage.CostEstimateGroupId);
            }

            if (stagesToSoftDelete.Count > 0)
            {
                List<Guid> softDeletedStageIds = stagesToSoftDelete.Select(s => s.Id).ToList();
                List<WorkScheduleStageWork> worksInSoftDeletedStages = (await workRepo.GetBySearch(
                    w => softDeletedStageIds.Contains(w.WorkScheduleStageId)))
                    .ToList();

                await DeleteObsoleteWorkScopesAsync(worksInSoftDeletedStages, workSchedule.Id, cancellationToken);
            }

            return stagesToSoftDelete.Select(s => s.Id).ToHashSet();
        }

        private static (List<CostEstimateGroup> RootGroups, Dictionary<Guid, List<CostEstimateGroup>> ChildGroupsByParent) BuildGroupHierarchy(
            List<CostEstimateGroup> allGroups)
        {
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParent = allGroups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

            List<CostEstimateGroup> rootGroups = allGroups
                .Where(g => g.ParentGroupId == null)
                .OrderBy(g => g.Order)
                .ToList();

            return (rootGroups, childGroupsByParent);
        }

        private async Task ProcessGroupsAsync(
            List<CostEstimateGroup> groups,
            Guid? parentStageId,
            WorkSchedule workSchedule,
            Dictionary<Guid, WorkScheduleStage> existingStagesByGroupId,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParent,
            List<WorkScheduleStage> resultStages,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                CostEstimateGroup group = groups[i];
                string name = ResolveGroupName(group, i + 1);
                WorkScheduleStage stage;

                if (existingStagesByGroupId.TryGetValue(group.Id, out WorkScheduleStage? existingStage))
                {
                    existingStage.Name = name;
                    existingStage.Order = i;
                    existingStage.ParentStageId = parentStageId;
                    await stageRepo.Update(existingStage);
                    stage = existingStage;
                }
                else
                {
                    stage = new WorkScheduleStage
                    {
                        TenantId = workSchedule.TenantId,
                        ProjectId = workSchedule.ProjectId,
                        WorkScheduleId = workSchedule.Id,
                        ParentStageId = parentStageId,
                        CostEstimateGroupId = group.Id,
                        Name = name,
                        Order = i
                    };
                    await stageRepo.Insert(stage);

                    logger.LogInformation(
                        "Created work schedule stage {StageId} for cost estimate group {GroupId} in work schedule {WorkScheduleId}",
                        stage.Id, group.Id, workSchedule.Id);
                }

                resultStages.Add(stage);

                if (childGroupsByParent.TryGetValue(group.Id, out List<CostEstimateGroup>? childGroups))
                {
                    await ProcessGroupsAsync(
                        childGroups, stage.Id, workSchedule,
                        existingStagesByGroupId, childGroupsByParent,
                        resultStages, cancellationToken);
                }
            }
        }

        private static string ResolveGroupName(CostEstimateGroup group, int order)
        {
            return !string.IsNullOrWhiteSpace(group.Name) ? group.Name : $"Nazwa etapu {order}";
        }

        private async Task SyncWorksFromItemsAsync(
            WorkSchedule workSchedule,
            Dictionary<Guid, WorkScheduleStage> stageByGroupId,
            Guid costEstimateId,
            CancellationToken cancellationToken)
        {
            List<CostEstimateItem> allItems = (await costEstimateItemRepo.GetBySearch(
                i => i.CostEstimateId == costEstimateId && !i.IsDeleted))
                .ToList();

            HashSet<Guid> stageIds = stageByGroupId.Values.Select(s => s.Id).ToHashSet();
            List<WorkScheduleStageWork> existingLinkedWorks = (await workRepo.GetBySearch(
                w => stageIds.Contains(w.WorkScheduleStageId) && w.CostEstimateItemId.HasValue))
                .ToList();

            Dictionary<Guid, WorkScheduleStageWork> existingWorkByItemId = existingLinkedWorks
                .ToDictionary(w => w.CostEstimateItemId!.Value);

            HashSet<Guid> activeItemIds = new HashSet<Guid>();

            Dictionary<Guid, List<CostEstimateItem>> itemsByGroupId = allItems
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

            foreach ((Guid groupId, List<CostEstimateItem> groupItems) in itemsByGroupId)
            {
                if (!stageByGroupId.TryGetValue(groupId, out WorkScheduleStage? stage))
                {
                    continue;
                }

                List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
                await UpsertWorkScopesForStageAsync(workSchedule, stage, workScopeItems, existingWorkByItemId, activeItemIds, cancellationToken);
            }

            List<WorkScheduleStageWork> worksToDelete = existingLinkedWorks
                .Where(w => !activeItemIds.Contains(w.CostEstimateItemId!.Value))
                .ToList();

            await DeleteObsoleteWorkScopesAsync(worksToDelete, workSchedule.Id, cancellationToken);
        }

        private async Task UpsertWorkScopesForStageAsync(
            WorkSchedule workSchedule,
            WorkScheduleStage stage,
            List<CostEstimateItem> workScopeItems,
            Dictionary<Guid, WorkScheduleStageWork> existingWorkByItemId,
            HashSet<Guid> activeItemIds,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < workScopeItems.Count; i++)
            {
                CostEstimateItem item = workScopeItems[i];
                activeItemIds.Add(item.Id);
                string name = ResolveItemName(item, i + 1);

                if (existingWorkByItemId.TryGetValue(item.Id, out WorkScheduleStageWork? existingWork))
                {
                    existingWork.Name = name;
                    existingWork.Order = i;
                    await workRepo.Update(existingWork);
                }
                else
                {
                    WorkScheduleStageWork newWork = new WorkScheduleStageWork
                    {
                        TenantId = workSchedule.TenantId,
                        ProjectId = workSchedule.ProjectId,
                        WorkScheduleStageId = stage.Id,
                        CostEstimateItemId = item.Id,
                        Name = name,
                        Order = i,
                        ColorRgb = DefaultWorkColorRgb
                    };
                    await workRepo.Insert(newWork);
                    await workRepo.SaveChangesAsync(cancellationToken);

                    await trackedCostRepo.ExecuteUpdateAsync(
                        tc => tc.CostEstimateItemId == item.Id
                              && tc.WorkScheduleStageWorkId == null,
                        tc => tc.SetProperty(p => p.WorkScheduleStageWorkId, newWork.Id),
                        cancellationToken);

                    logger.LogInformation(
                        "Created work scope {WorkId} for cost estimate item {ItemId} in stage {StageId}",
                        newWork.Id, item.Id, stage.Id);
                }
            }
        }

        private async Task DeleteObsoleteWorkScopesAsync(
            List<WorkScheduleStageWork> worksToDelete,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            if (worksToDelete.Count == 0)
            {
                return;
            }

            List<Guid> deletedWorkIds = worksToDelete.Select(w => w.Id).ToList();
            await DeleteDependenciesForWorksAsync(deletedWorkIds.ToHashSet(), workScheduleId, cancellationToken);

            await trackedCostRepo.ExecuteUpdateAsync(
                tc => deletedWorkIds.Contains(tc.WorkScheduleStageWorkId!.Value),
                tc => tc.SetProperty(p => p.WorkScheduleStageWorkId, (Guid?)null),
                cancellationToken);

            List<Guid> deletedItemIds = worksToDelete
                .Where(w => w.CostEstimateItemId.HasValue)
                .Select(w => w.CostEstimateItemId!.Value)
                .ToList();

            if (deletedItemIds.Count > 0)
            {
                await trackedCostRepo.ExecuteUpdateAsync(
                    tc => deletedItemIds.Contains(tc.CostEstimateItemId!.Value)
                          && tc.WorkScheduleStageWorkId == null,
                    tc => tc.SetProperty(p => p.CostEstimateItemId, (Guid?)null),
                    cancellationToken);
            }

            DateTime now = DateTime.UtcNow;
            foreach (WorkScheduleStageWork work in worksToDelete)
            {
                work.IsDeleted = true;
                work.DeletedAt = now;
                logger.LogInformation(
                    "Soft-deleted work scope {WorkId} — linked cost estimate item {ItemId} is no longer a work scope",
                    work.Id, work.CostEstimateItemId);
            }
            await workRepo.UpdateRange(worksToDelete);
            await workRepo.SaveChangesAsync(cancellationToken);
        }

        private static bool IsWorkScopeItem(CostEstimateItem item)
        {
            return item.IsStageWork && item.RelationType == ItemRelationType.None;
        }

        private static string ResolveItemName(CostEstimateItem item, int order)
        {
            return !string.IsNullOrWhiteSpace(item.Name) ? item.Name : $"Zakres pracy {order}";
        }

        private async Task DeleteDependenciesForWorksAsync(
            HashSet<Guid> workIds,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            if (workIds.Count == 0)
            {
                return;
            }

            List<WorkScheduleStageWorkDependency> affectedDependencies = (await dependencyRepo.GetBySearch(
                d => d.WorkScheduleId == workScheduleId
                     && (workIds.Contains(d.PredecessorWorkId) || workIds.Contains(d.SuccessorWorkId))))
                .ToList();

            if (affectedDependencies.Count > 0)
            {
                await dependencyRepo.DeleteRange(affectedDependencies);
            }
        }
    }
}
