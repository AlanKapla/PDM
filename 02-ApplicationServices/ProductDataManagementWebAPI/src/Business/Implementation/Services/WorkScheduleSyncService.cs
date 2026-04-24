using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Entities.Models.WorkItemLinks;
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
        private readonly IWorkItemLinkService workItemLinkService;
        private readonly ILogger<WorkScheduleSyncService> logger;

        private const string DefaultWorkColorRgb = "#3B82F6";

        public WorkScheduleSyncService(
            IRepository<CostEstimateGroup> costEstimateGroupRepo,
            IRepository<CostEstimateItem> costEstimateItemRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IWorkItemLinkService workItemLinkService,
            ILogger<WorkScheduleSyncService> logger)
        {
            this.costEstimateGroupRepo = costEstimateGroupRepo;
            this.costEstimateItemRepo = costEstimateItemRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.dependencyRepo = dependencyRepo;
            this.workItemLinkService = workItemLinkService;
            this.logger = logger;
        }

        public async Task<List<WorkScheduleStage>> SyncFromCostEstimateAsync(
            WorkSchedule workSchedule,
            CancellationToken cancellationToken)
        {
            CostEstimateWorkScheduleLink? workScheduleLink = await workItemLinkService.GetWorkScheduleLinkAsync(
                workSchedule.Id, cancellationToken);

            if (workScheduleLink?.CostEstimateId == null)
                throw new InvalidOperationException(
                    $"WorkSchedule {workSchedule.Id} is not linked to a cost estimate.");

            Guid costEstimateId = workScheduleLink.CostEstimateId.Value;
            Guid workScheduleLinkId = workScheduleLink.Id;

            List<CostEstimateGroup> allGroups = (await costEstimateGroupRepo.GetBySearch(
                g => g.CostEstimateId == costEstimateId && !g.IsDeleted,
                include => include
                    .Include(g => g.FieldValues)
                    .ThenInclude(fv => fv.FieldDefinition)))
                .ToList();

            List<WorkScheduleStage> allStages = (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workSchedule.Id && !s.IsDeleted))
                .ToList();

            IReadOnlyList<CostEstimateGroupWorkScheduleStageLink> existingGroupStageLinks =
                await workItemLinkService.GetGroupStageLinksForWorkScheduleLinkAsync(
                    workScheduleLinkId, cancellationToken);

            HashSet<Guid> activeGroupIds = allGroups.Select(g => g.Id).ToHashSet();
            HashSet<Guid> softDeletedStageIds = await SoftDeleteObsoleteStagesAsync(
                allStages, existingGroupStageLinks, activeGroupIds, workSchedule, cancellationToken);

            HashSet<Guid> allStageIds = allStages.Select(s => s.Id).ToHashSet();
            Dictionary<Guid, WorkScheduleStage> existingStagesByGroupId = BuildStageMappingFromLinks(
                existingGroupStageLinks, allStageIds, softDeletedStageIds, allStages);

            (List<CostEstimateGroup> rootGroups, Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParent) =
                BuildGroupHierarchy(allGroups);

            List<WorkScheduleStage> resultStages = new List<WorkScheduleStage>();

            await ProcessGroupsAsync(
                rootGroups, null, workSchedule,
                workScheduleLinkId,
                existingStagesByGroupId, childGroupsByParent,
                resultStages, cancellationToken);

            await stageRepo.SaveChangesAsync(cancellationToken);

            IReadOnlyList<CostEstimateGroupWorkScheduleStageLink> freshGroupStageLinks =
                await workItemLinkService.GetGroupStageLinksForWorkScheduleLinkAsync(
                    workScheduleLinkId, cancellationToken);

            HashSet<Guid> resultStageIds = resultStages.Select(s => s.Id).ToHashSet();
            Dictionary<Guid, WorkScheduleStage> stageByGroupId = BuildStageMappingFromLinks(
                freshGroupStageLinks, resultStageIds, new HashSet<Guid>(), resultStages);

            await SyncWorksFromItemsAsync(workSchedule, stageByGroupId, freshGroupStageLinks, costEstimateId, cancellationToken);
            await workRepo.SaveChangesAsync(cancellationToken);

            return resultStages;
        }

        private async Task<HashSet<Guid>> SoftDeleteObsoleteStagesAsync(
            List<WorkScheduleStage> allStages,
            IReadOnlyList<CostEstimateGroupWorkScheduleStageLink> groupStageLinks,
            HashSet<Guid> activeGroupIds,
            WorkSchedule workSchedule,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> allStageIds = allStages.Select(s => s.Id).ToHashSet();

            Dictionary<Guid, CostEstimateGroupWorkScheduleStageLink> stageLinkByStageId = groupStageLinks
                .Where(l => allStageIds.Contains(l.WorkScheduleStageId!.Value))
                .ToDictionary(l => l.WorkScheduleStageId!.Value);

            List<WorkScheduleStage> stagesToSoftDelete = allStages
                .Where(s => stageLinkByStageId.TryGetValue(s.Id, out CostEstimateGroupWorkScheduleStageLink? link)
                            && !activeGroupIds.Contains(link.CostEstimateGroupId!.Value))
                .ToList();

            DateTime now = DateTime.UtcNow;
            foreach (WorkScheduleStage stage in stagesToSoftDelete)
            {
                Guid? groupId = stageLinkByStageId[stage.Id].CostEstimateGroupId;
                stage.IsDeleted = true;
                stage.DeletedAt = now;
                await stageRepo.Update(stage);
                logger.LogInformation(
                    "Soft-deleted work schedule stage {StageId} — linked cost estimate group {GroupId} is no longer active",
                    stage.Id, groupId);
            }

            if (stagesToSoftDelete.Count > 0)
            {
                List<Guid> softDeletedStageIds = stagesToSoftDelete.Select(s => s.Id).ToList();
                List<WorkScheduleStageWork> worksInSoftDeletedStages = (await workRepo.GetBySearch(
                    w => softDeletedStageIds.Contains(w.WorkScheduleStageId)))
                    .ToList();

                await DeleteObsoleteWorkScopesAsync(worksInSoftDeletedStages, workSchedule.Id, cancellationToken);
                await workItemLinkService.DeleteGroupStageLinksForStagesAsync(
                    softDeletedStageIds, cancellationToken);
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

        private static Dictionary<Guid, WorkScheduleStage> BuildStageMappingFromLinks(
            IReadOnlyList<CostEstimateGroupWorkScheduleStageLink> groupStageLinks,
            HashSet<Guid> includedStageIds,
            HashSet<Guid> excludedStageIds,
            List<WorkScheduleStage> stages)
        {
            return groupStageLinks
                .Where(l => l.WorkScheduleStageId.HasValue
                            && includedStageIds.Contains(l.WorkScheduleStageId!.Value)
                            && !excludedStageIds.Contains(l.WorkScheduleStageId!.Value))
                .ToDictionary(
                    l => l.CostEstimateGroupId!.Value,
                    l => stages.First(s => s.Id == l.WorkScheduleStageId!.Value));
        }

        private async Task ProcessGroupsAsync(
            List<CostEstimateGroup> groups,
            Guid? parentStageId,
            WorkSchedule workSchedule,
            Guid workScheduleLinkId,
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
                        Name = name,
                        Order = i
                    };
                    await stageRepo.Insert(stage);
                    // SaveChangesAsync required here: child stages need this stage's Id as ParentStageId
                    await stageRepo.SaveChangesAsync(cancellationToken);

                    // Utwórz łącznik grupy kosztorysu z etapem harmonogramu
                    await workItemLinkService.CreateGroupStageLinkForScheduleStageAsync(
                        workSchedule.Id, stage.Id, group.Id, cancellationToken);

                    logger.LogInformation(
                        "Created work schedule stage {StageId} for cost estimate group {GroupId} in work schedule {WorkScheduleId}",
                        stage.Id, group.Id, workSchedule.Id);
                }

                resultStages.Add(stage);

                if (childGroupsByParent.TryGetValue(group.Id, out List<CostEstimateGroup>? childGroups))
                {
                    await ProcessGroupsAsync(
                        childGroups, stage.Id, workSchedule,
                        workScheduleLinkId,
                        existingStagesByGroupId, childGroupsByParent,
                        resultStages, cancellationToken);
                }
            }
        }

        private static string ResolveGroupName(CostEstimateGroup group, int order)
        {
            string? nameValue = group.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition.FieldType == FieldType.GroupName)
                ?.StringValue;

            return !string.IsNullOrWhiteSpace(nameValue) ? nameValue : $"Nazwa etapu {order}";
        }

        private async Task SyncWorksFromItemsAsync(
            WorkSchedule workSchedule,
            Dictionary<Guid, WorkScheduleStage> stageByGroupId,
            IReadOnlyList<CostEstimateGroupWorkScheduleStageLink> groupStageLinks,
            Guid costEstimateId,
            CancellationToken cancellationToken)
        {
            Dictionary<Guid, Guid> groupStageLinkIdByGroupId = groupStageLinks
                .Where(l => l.CostEstimateGroupId.HasValue)
                .ToDictionary(l => l.CostEstimateGroupId!.Value, l => l.Id);
            List<CostEstimateItem> allItems = (await costEstimateItemRepo.GetBySearch(
                i => i.CostEstimateId == costEstimateId && !i.IsDeleted,
                include => include
                    .Include(i => i.FieldValues)
                    .ThenInclude(fv => fv.FieldDefinition)))
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
                    continue;

                if (!groupStageLinkIdByGroupId.TryGetValue(groupId, out Guid groupStageLinkId))
                    continue;

                List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
                await UpsertWorkScopesForStageAsync(workSchedule, stage, groupStageLinkId, workScopeItems, existingWorkByItemId, activeItemIds, cancellationToken);
            }

            List<WorkScheduleStageWork> worksToDelete = existingLinkedWorks
                .Where(w => !activeItemIds.Contains(w.CostEstimateItemId!.Value))
                .ToList();

            await DeleteObsoleteWorkScopesAsync(worksToDelete, workSchedule.Id, cancellationToken);
        }

        private async Task UpsertWorkScopesForStageAsync(
            WorkSchedule workSchedule,
            WorkScheduleStage stage,
            Guid groupStageLinkId,
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
                Guid workId;

                if (existingWorkByItemId.TryGetValue(item.Id, out WorkScheduleStageWork? existingWork))
                {
                    existingWork.Name = name;
                    existingWork.Order = i;
                    await workRepo.Update(existingWork);
                    workId = existingWork.Id;
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
                    workId = newWork.Id;
                    logger.LogInformation(
                        "Created work scope {WorkId} for cost estimate item {ItemId} in stage {StageId}",
                        newWork.Id, item.Id, stage.Id);
                }

                await workItemLinkService.UpsertWorkItemLinkAsync(
                    workSchedule.ProjectId,
                    groupStageLinkId,
                    item.Id,
                    workId,
                    name,
                    item.NetValue,
                    item.GrossValue,
                    i,
                    cancellationToken);
            }
        }

        private async Task DeleteObsoleteWorkScopesAsync(
            List<WorkScheduleStageWork> worksToDelete,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            if (worksToDelete.Count == 0)
                return;

            List<Guid> deletedWorkIds = worksToDelete.Select(w => w.Id).ToList();
            await workItemLinkService.DeleteWorkItemLinksForWorksAsync(
                deletedWorkIds, cancellationToken);
            await DeleteDependenciesForWorksAsync(deletedWorkIds.ToHashSet(), workScheduleId, cancellationToken);
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
            string? nameValue = item.FieldValues
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

            List<WorkScheduleStageWorkDependency> affectedDependencies = (await dependencyRepo.GetBySearch(
                d => d.WorkScheduleId == workScheduleId
                     && (workIds.Contains(d.PredecessorWorkId) || workIds.Contains(d.SuccessorWorkId))))
                .ToList();

            if (affectedDependencies.Count > 0)
                await dependencyRepo.DeleteRange(affectedDependencies);
        }
    }
}
