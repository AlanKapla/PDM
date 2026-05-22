using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.CostEstimates;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.GetCostLinkOptions
{
    public sealed class GetCostLinkOptionsQueryHandler
        : IRequestHandler<GetCostLinkOptionsQuery, CostLinkOptionsWeb>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<CostEstimateGroup> groupRepository;
        private readonly IReadRepository<CostEstimateItem> itemRepository;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly IReadRepository<WorkScheduleStage> stageRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;

        public GetCostLinkOptionsQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateGroup> groupRepository,
            IReadRepository<CostEstimateItem> itemRepository,
            IReadRepository<WorkSchedule> workScheduleRepository,
            IReadRepository<WorkScheduleStage> stageRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.stageRepository = stageRepository;
            this.stageWorkRepository = stageWorkRepository;
        }

        public async Task<CostLinkOptionsWeb> Handle(
            GetCostLinkOptionsQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<CostEstimate> estimates = await costEstimateRepository.GetBySearch(
                ce => ce.TenantId == request.TenantId && ce.ProjectId == request.ProjectId);

            HashSet<Guid> estimateIds = estimates.Select(e => e.Id).ToHashSet();
            Dictionary<Guid, string> estimateNamesById = estimates.ToDictionary(e => e.Id, e => e.Name);

            IEnumerable<WorkSchedule> schedules = await workScheduleRepository.GetBySearch(
                ws => ws.TenantId == request.TenantId && ws.ProjectId == request.ProjectId);

            HashSet<Guid> scheduleIds = schedules.Select(s => s.Id).ToHashSet();
            Dictionary<Guid, string> scheduleNamesById = schedules.ToDictionary(s => s.Id, s => s.Name);

            List<EstimateItemLinkOptionWeb> estimateItems = new List<EstimateItemLinkOptionWeb>();
            List<WorkLinkOptionWeb> workItems = new List<WorkLinkOptionWeb>();

            if (estimateIds.Count > 0)
            {
                IEnumerable<CostEstimateGroup> allGroups = await groupRepository.GetBySearch(
                    g => estimateIds.Contains(g.CostEstimateId));

                Dictionary<Guid, CostEstimateGroup> groupsById = allGroups.ToDictionary(g => g.Id);

                IEnumerable<CostEstimateItem> allItems = await itemRepository.GetBySearch(
                    i => estimateIds.Contains(i.CostEstimateId)
                         && i.RelationType == ItemRelationType.None);

                estimateItems = allItems
                    .Select(item => new EstimateItemLinkOptionWeb
                    {
                        ItemId = item.Id,
                        Path = BuildItemPath(item, groupsById, estimateNamesById),
                        LinkedWorkId = null
                    })
                    .ToList();
            }

            if (scheduleIds.Count > 0)
            {
                IEnumerable<WorkScheduleStage> allStages = await stageRepository.GetBySearch(
                    s => s.TenantId == request.TenantId
                         && s.ProjectId == request.ProjectId
                         && scheduleIds.Contains(s.WorkScheduleId));

                Dictionary<Guid, WorkScheduleStage> stagesById = allStages.ToDictionary(s => s.Id);
                Dictionary<Guid, Guid> scheduleIdByStageId = allStages
                    .ToDictionary(s => s.Id, s => s.WorkScheduleId);

                HashSet<Guid> stageIds = allStages.Select(s => s.Id).ToHashSet();

                if (stageIds.Count > 0)
                {
                    IEnumerable<WorkScheduleStageWork> allWorks = await stageWorkRepository.GetBySearch(
                        w => w.TenantId == request.TenantId
                             && w.ProjectId == request.ProjectId
                             && stageIds.Contains(w.WorkScheduleStageId));

                    // Build cross-reference: itemId -> first workId linked to it
                    Dictionary<Guid, Guid> workIdByItemId = allWorks
                        .Where(w => w.CostEstimateItemId.HasValue)
                        .GroupBy(w => w.CostEstimateItemId!.Value)
                        .ToDictionary(g => g.Key, g => g.First().Id);

                    // Fill LinkedWorkId on estimate items
                    estimateItems = estimateItems
                        .Select(opt => workIdByItemId.TryGetValue(opt.ItemId, out Guid linkedWorkId)
                            ? opt with { LinkedWorkId = linkedWorkId }
                            : opt)
                        .ToList();

                    workItems = allWorks
                        .Select(work =>
                        {
                            Guid scheduleId = scheduleIdByStageId.TryGetValue(work.WorkScheduleStageId, out Guid sid) ? sid : Guid.Empty;
                            return new WorkLinkOptionWeb
                            {
                                WorkId = work.Id,
                                Path = BuildWorkPath(work, stagesById, scheduleNamesById, scheduleId),
                                LinkedItemId = work.CostEstimateItemId
                            };
                        })
                        .ToList();
                }
            }

            return new CostLinkOptionsWeb
            {
                EstimateItems = estimateItems,
                WorkItems = workItems
            };
        }

        private static string BuildItemPath(
            CostEstimateItem item,
            Dictionary<Guid, CostEstimateGroup> groupsById,
            Dictionary<Guid, string> estimateNamesById)
        {
            List<string> parts = new List<string> { item.Name };

            if (groupsById.TryGetValue(item.GroupId, out CostEstimateGroup? group))
            {
                BuildGroupPathParts(group, groupsById, parts);
            }

            if (estimateNamesById.TryGetValue(item.CostEstimateId, out string? estimateName))
            {
                parts.Add(estimateName);
            }

            parts.Reverse();
            return string.Join(" > ", parts);
        }

        private static void BuildGroupPathParts(
            CostEstimateGroup group,
            Dictionary<Guid, CostEstimateGroup> groupsById,
            List<string> parts)
        {
            parts.Add(group.Name);

            if (group.ParentGroupId.HasValue
                && groupsById.TryGetValue(group.ParentGroupId.Value, out CostEstimateGroup? parent))
            {
                BuildGroupPathParts(parent, groupsById, parts);
            }
        }

        private static string BuildWorkPath(
            WorkScheduleStageWork work,
            Dictionary<Guid, WorkScheduleStage> stagesById,
            Dictionary<Guid, string> scheduleNamesById,
            Guid scheduleId)
        {
            List<string> parts = new List<string> { work.Name };

            if (stagesById.TryGetValue(work.WorkScheduleStageId, out WorkScheduleStage? stage))
            {
                BuildStagePathParts(stage, stagesById, parts);
            }

            if (scheduleNamesById.TryGetValue(scheduleId, out string? scheduleName))
            {
                parts.Add(scheduleName);
            }

            parts.Reverse();
            return string.Join(" > ", parts);
        }

        private static void BuildStagePathParts(
            WorkScheduleStage stage,
            Dictionary<Guid, WorkScheduleStage> stagesById,
            List<string> parts)
        {
            parts.Add(stage.Name);

            if (stage.ParentStageId.HasValue
                && stagesById.TryGetValue(stage.ParentStageId.Value, out WorkScheduleStage? parent))
            {
                BuildStagePathParts(parent, stagesById, parts);
            }
        }
    }
}
