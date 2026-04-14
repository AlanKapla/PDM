using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
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
        private readonly ILogger<WorkScheduleSyncService> logger;

        private const string DefaultWorkColorRgb = "#3B82F6";

        public WorkScheduleSyncService(
            IRepository<CostEstimateGroup> costEstimateGroupRepo,
            IRepository<CostEstimateItem> costEstimateItemRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            ILogger<WorkScheduleSyncService> logger)
        {
            this.costEstimateGroupRepo = costEstimateGroupRepo;
            this.costEstimateItemRepo = costEstimateItemRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.dependencyRepo = dependencyRepo;
            this.logger = logger;
        }

        public async Task<List<WorkScheduleStage>> SyncFromCostEstimateAsync(
            WorkSchedule workSchedule,
            CancellationToken cancellationToken)
        {
            if (!workSchedule.CostEstimateId.HasValue)
                throw new InvalidOperationException(
                    $"WorkSchedule {workSchedule.Id} is not linked to a cost estimate.");

            var costEstimateId = workSchedule.CostEstimateId.Value;

            // Load all non-deleted groups from the cost estimate with their GroupName field values
            var allGroups = (await costEstimateGroupRepo.GetBySearch(
                g => g.CostEstimateId == costEstimateId && !g.IsDeleted,
                include => include
                    .Include(g => g.FieldValues)
                    .ThenInclude(fv => fv.FieldDefinition)))
                .ToList();

            // Load existing stages linked to this cost estimate (not soft-deleted)
            var existingLinkedStages = (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workSchedule.Id
                     && !s.IsDeleted
                     && s.CostEstimateGroupId != null))
                .ToList();

            var activeGroupIds = allGroups.Select(g => g.Id).ToHashSet();

            // Soft-delete stages whose cost estimate groups have been deleted
            List<WorkScheduleStage> stagesToSoftDelete = existingLinkedStages
                .Where(s => !activeGroupIds.Contains(s.CostEstimateGroupId!.Value))
                .ToList();

            foreach (var stage in stagesToSoftDelete)
            {
                stage.IsDeleted = true;
                stage.DeletedAt = DateTime.UtcNow;
                await stageRepo.Update(stage);
                logger.LogInformation(
                    "Soft-deleted work schedule stage {StageId} — linked cost estimate group {GroupId} is no longer active",
                    stage.Id, stage.CostEstimateGroupId);
            }

            if (stagesToSoftDelete.Count > 0)
            {
                var softDeletedStageIds = stagesToSoftDelete.Select(s => s.Id).ToHashSet();
                var worksInSoftDeletedStages = (await workRepo.GetBySearch(
                    w => softDeletedStageIds.Contains(w.WorkScheduleStageId)))
                    .ToList();

                await DeleteObsoleteWorkScopesAsync(worksInSoftDeletedStages, workSchedule.Id, cancellationToken);
            }

            var existingStagesByGroupId = existingLinkedStages
                .Where(s => !s.IsDeleted)
                .ToDictionary(s => s.CostEstimateGroupId!.Value);

            // Build group parent-child lookup
            var childGroupsByParent = allGroups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

            var rootGroups = allGroups
                .Where(g => g.ParentGroupId == null)
                .OrderBy(g => g.Order)
                .ToList();

            var resultStages = new List<WorkScheduleStage>();

            await ProcessGroupsAsync(
                rootGroups, null, workSchedule,
                existingStagesByGroupId, childGroupsByParent,
                resultStages, cancellationToken);

            await stageRepo.SaveChangesAsync(cancellationToken);

            await SyncWorksFromItemsAsync(workSchedule, resultStages, costEstimateId, cancellationToken);

            await workRepo.SaveChangesAsync(cancellationToken);

            return resultStages;
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
                var group = groups[i];
                var name = ResolveGroupName(group, i + 1);
                WorkScheduleStage stage;

                if (existingStagesByGroupId.TryGetValue(group.Id, out var existingStage))
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
                        CostEstimateGroupId = group.Id,
                        ParentStageId = parentStageId,
                        Name = name,
                        Order = i
                    };
                    await stageRepo.Insert(stage);
                    // SaveChangesAsync required here: child stages need this stage's Id as ParentStageId
                    await stageRepo.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Created work schedule stage {StageId} for cost estimate group {GroupId} in work schedule {WorkScheduleId}",
                        stage.Id, group.Id, workSchedule.Id);
                }

                resultStages.Add(stage);

                if (childGroupsByParent.TryGetValue(group.Id, out var childGroups))
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
            var nameValue = group.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition.FieldType == FieldType.GroupName)
                ?.StringValue;

            return !string.IsNullOrWhiteSpace(nameValue) ? nameValue : $"Nazwa etapu {order}";
        }

        private async Task SyncWorksFromItemsAsync(
            WorkSchedule workSchedule,
            List<WorkScheduleStage> stages,
            Guid costEstimateId,
            CancellationToken cancellationToken)
        {
            var allItems = (await costEstimateItemRepo.GetBySearch(
                i => i.CostEstimateId == costEstimateId && !i.IsDeleted,
                include => include
                    .Include(i => i.FieldValues)
                    .ThenInclude(fv => fv.FieldDefinition)))
                .ToList();

            var stageByGroupId = stages
                .Where(s => s.CostEstimateGroupId.HasValue)
                .ToDictionary(s => s.CostEstimateGroupId!.Value);

            var stageIds = stages.Select(s => s.Id).ToHashSet();
            var existingLinkedWorks = (await workRepo.GetBySearch(
                w => stageIds.Contains(w.WorkScheduleStageId) && w.CostEstimateItemId.HasValue))
                .ToList();

            var existingWorkByItemId = existingLinkedWorks
                .ToDictionary(w => w.CostEstimateItemId!.Value);

            var activeItemIds = new HashSet<Guid>();

            var itemsByGroupId = allItems
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

            foreach (var (groupId, groupItems) in itemsByGroupId)
            {
                if (!stageByGroupId.TryGetValue(groupId, out var stage))
                    continue;

                var workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
                await UpsertWorkScopesForStageAsync(workSchedule, stage, workScopeItems, existingWorkByItemId, activeItemIds);
            }

            var worksToDelete = existingLinkedWorks
                .Where(w => !activeItemIds.Contains(w.CostEstimateItemId!.Value))
                .ToList();

            await DeleteObsoleteWorkScopesAsync(worksToDelete, workSchedule.Id, cancellationToken);
        }

        private async Task UpsertWorkScopesForStageAsync(
            WorkSchedule workSchedule,
            WorkScheduleStage stage,
            List<CostEstimateItem> workScopeItems,
            Dictionary<Guid, WorkScheduleStageWork> existingWorkByItemId,
            HashSet<Guid> activeItemIds)
        {
            for (int i = 0; i < workScopeItems.Count; i++)
            {
                var item = workScopeItems[i];
                activeItemIds.Add(item.Id);
                var name = ResolveItemName(item, i + 1);

                if (existingWorkByItemId.TryGetValue(item.Id, out var existingWork))
                {
                    existingWork.Name = name;
                    existingWork.Order = i;
                    await workRepo.Update(existingWork);
                }
                else
                {
                    var newWork = new WorkScheduleStageWork
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
                return;

            var deletedWorkIds = worksToDelete.Select(w => w.Id).ToHashSet();
            await DeleteDependenciesForWorksAsync(deletedWorkIds, workScheduleId, cancellationToken);
            await workRepo.DeleteRange(worksToDelete);

            foreach (var w in worksToDelete)
            {
                logger.LogInformation(
                    "Deleted work scope {WorkId} — linked cost estimate item {ItemId} is no longer a work scope",
                    w.Id, w.CostEstimateItemId);
            }
        }

        private static bool IsWorkScopeItem(CostEstimateItem item)
        {
            return item.FieldValues.Any(fv =>
                fv.FieldDefinition.FieldType == FieldType.ItemSystemIsWorkScope &&
                fv.BoolValue == true);
        }

        private static string ResolveItemName(CostEstimateItem item, int order)
        {
            var nameValue = item.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition.FieldType == FieldType.ItemSystemName)
                ?.StringValue;

            return !string.IsNullOrWhiteSpace(nameValue) ? nameValue : $"Zakres pracy {order}";
        }

        private async Task DeleteDependenciesForWorksAsync(
            HashSet<Guid> workIds,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            if (workIds.Count == 0)
                return;

            List<WorkScheduleStageWorkDependency> affected = (await dependencyRepo.GetBySearch(
                d => d.WorkScheduleId == workScheduleId
                     && (workIds.Contains(d.PredecessorWorkId) || workIds.Contains(d.SuccessorWorkId))))
                .ToList();

            if (affected.Count > 0)
                await dependencyRepo.DeleteRange(affected);
        }
    }
}
