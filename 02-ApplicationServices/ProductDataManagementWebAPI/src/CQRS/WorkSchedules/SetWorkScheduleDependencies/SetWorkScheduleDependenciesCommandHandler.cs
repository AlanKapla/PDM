using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleDependencies
{
    public sealed class SetWorkScheduleDependenciesCommandHandler : IRequestHandler<SetWorkScheduleDependenciesCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepository;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IUserService userService;

        public SetWorkScheduleDependenciesCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepository,
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IUserService userService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workRepository = workRepository;
            this.dependencyRepository = dependencyRepository;
            this.periodRepository = periodRepository;
            this.userService = userService;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(SetWorkScheduleDependenciesCommand request, CancellationToken cancellationToken)
        {
            HashSet<Guid> workIds = await workRepository.SelectToHashSetAsync(
                w => w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId
                  && w.Stage.WorkScheduleId == request.WorkScheduleId,
                w => w.Id,
                cancellationToken);

            ValidateAllDependencyIdsExist(request.Dependencies, workIds);

            await AdjustSuccessorPeriodsAsync(request, cancellationToken);

            IEnumerable<WorkScheduleStageWorkDependency> existing = await dependencyRepository.GetBySearch(
                d => d.WorkScheduleId == request.WorkScheduleId
                  && d.TenantId == request.TenantId);

            Dictionary<(Guid, Guid), WorkScheduleStageWorkDependency> existingByKey = existing
                .ToDictionary(d => (d.PredecessorWorkId, d.SuccessorWorkId));

            Dictionary<(Guid, Guid), WorkDependencyDto> incomingByKey = request.Dependencies
                .ToDictionary(dto => (dto.PredecessorWorkId, dto.SuccessorWorkId));

            List<WorkScheduleStageWorkDependency> toDelete = existingByKey
                .Where(kv => !incomingByKey.ContainsKey(kv.Key))
                .Select(kv => kv.Value)
                .ToList();

            List<WorkScheduleStageWorkDependency> toAdd = incomingByKey
                .Where(kv => !existingByKey.ContainsKey(kv.Key))
                .Select(kv => new WorkScheduleStageWorkDependency
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    WorkScheduleId = request.WorkScheduleId,
                    PredecessorWorkId = kv.Value.PredecessorWorkId,
                    SuccessorWorkId = kv.Value.SuccessorWorkId,
                    DependencyType = kv.Value.DependencyType,
                    LagDays = kv.Value.LagDays
                })
                .ToList();

            List<WorkScheduleStageWorkDependency> toUpdate = incomingByKey
                .Where(kv => existingByKey.ContainsKey(kv.Key))
                .Select(kv =>
                {
                    WorkScheduleStageWorkDependency dep = existingByKey[kv.Key];
                    dep.DependencyType = kv.Value.DependencyType;
                    dep.LagDays = kv.Value.LagDays;
                    return dep;
                })
                .ToList();

            if (toDelete.Count > 0)
            {
                await dependencyRepository.DeleteRange(toDelete);
            }

            if (toAdd.Count > 0)
            {
                await dependencyRepository.InsertRange(toAdd);
            }

            if (toUpdate.Count > 0)
            {
                await dependencyRepository.UpdateRange(toUpdate);
            }

            await dependencyRepository.SaveChangesAsync(cancellationToken);

            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId
                   && ws.TenantId == request.TenantId
                   && ws.ProjectId == request.ProjectId
                   && !ws.IsDeleted,
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Periods),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Comments),
                include => include
                    .Include(ws => ws.Dependencies))
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            var membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            List<WorkScheduleStage> activeStages = workSchedule.Stages.Where(s => !s.IsDeleted).ToList();

            List<WorkScheduleStageWeb> BuildStageTree(Guid? parentId)
            {
                return activeStages
                    .Where(s => s.ParentStageId == parentId)
                    .OrderBy(s => s.Order)
                    .Select(s => new WorkScheduleStageWeb(
                        Id: s.Id,
                        Name: s.Name,
                        Order: s.Order,
                        ParentStageId: s.ParentStageId,
                        CostEstimateGroupId: s.CostEstimateGroupId,
                        Works: s.Works
                            .OrderBy(w => w.Order)
                            .Select(w => new WorkScheduleStageWorkWeb(
                                Id: w.Id,
                                Name: w.Name,
                                Order: w.Order,
                                ColorRgb: w.ColorRgb,
                                IsClosed: w.Periods.Any() && w.Periods.All(p => p.IsClosed),
                                PlannedStartDate: w.PlannedStartDate,
                                PlannedEndDate: w.PlannedEndDate,
                                Periods: w.Periods
                                    .OrderBy(p => p.StartDate)
                                    .Select(p => new WorkScheduleStageWorkPeriodWeb(
                                        Id: p.Id,
                                        StartDate: p.StartDate,
                                        EndDate: p.EndDate,
                                        IsClosed: p.IsClosed))
                                    .ToList(),
                                Assignees: w.Assignments
                                    .Select(a => new WorkScheduleStageWorkAssigneeWeb(
                                        UserId: a.UserId,
                                        UserName: membersDict.TryGetValue(a.UserId, out var assignee)
                                            ? assignee.FullName
                                            : "Unknown"))
                                    .ToList(),
                                Comments: w.Comments
                                    .OrderBy(c => c.CreatedAt)
                                    .Select(c => new WorkScheduleStageWorkCommentWeb(
                                        Id: c.Id,
                                        Content: c.Content,
                                        CreatedByUserId: c.CreatedByUserId,
                                        CreatedByUserName: membersDict.TryGetValue(c.CreatedByUserId, out var commenter)
                                            ? commenter.FullName
                                            : "Unknown",
                                        CreatedAt: c.CreatedAt))
                                    .ToList()))
                            .ToList(),
                        ChildStages: BuildStageTree(s.Id)))
                    .ToList();
            }

            return new WorkScheduleDetailsWeb(
                Id: workSchedule.Id,
                TenantId: workSchedule.TenantId,
                ProjectId: workSchedule.ProjectId,
                CostEstimateId: workSchedule.CostEstimateId,
                Name: workSchedule.Name,
                CreatedAt: workSchedule.CreatedAt,
                CreatedByUserId: workSchedule.CreatedByUserId,
                CreatedByUserName: membersDict.TryGetValue(workSchedule.CreatedByUserId, out var creator)
                    ? creator.FullName
                    : "Unknown",
                Stages: BuildStageTree(null),
                Dependencies: workSchedule.Dependencies
                    .Select(d => new WorkScheduleWorkDependencyWeb(
                        Id: d.Id,
                        PredecessorWorkId: d.PredecessorWorkId,
                        SuccessorWorkId: d.SuccessorWorkId,
                        DependencyType: d.DependencyType,
                        LagDays: d.LagDays))
                    .ToList());
        }

        private async Task AdjustSuccessorPeriodsAsync(
            SetWorkScheduleDependenciesCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Dependencies.Count == 0)
                return;

            HashSet<Guid> involvedIds = request.Dependencies
                .SelectMany(d => new[] { d.PredecessorWorkId, d.SuccessorWorkId })
                .ToHashSet();

            IEnumerable<WorkScheduleStageWork> works = await workRepository.GetBySearch(
                w => involvedIds.Contains(w.Id),
                include => include.Include(w => w.Periods));

            Dictionary<Guid, WorkScheduleStageWork> workById = works.ToDictionary(w => w.Id);

            // predecessorId → its successors (for topological traversal)
            Dictionary<Guid, List<Guid>> successorsByPred = request.Dependencies
                .GroupBy(d => d.PredecessorWorkId)
                .ToDictionary(g => g.Key, g => g.Select(d => d.SuccessorWorkId).ToList());

            // successorId → all deps where this work is the successor
            Dictionary<Guid, List<WorkDependencyDto>> predsBySuccessor = request.Dependencies
                .GroupBy(d => d.SuccessorWorkId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Kahn's algorithm: process each work only after all its predecessors are processed,
            // so shifts propagate correctly through the chain (A→B→C: C sees B's already-shifted dates).
            Dictionary<Guid, int> inDegree = involvedIds.ToDictionary(id => id, _ => 0);
            foreach (WorkDependencyDto dep in request.Dependencies)
                inDegree[dep.SuccessorWorkId]++;

            Queue<Guid> queue = new Queue<Guid>(
                inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

            while (queue.Count > 0)
            {
                Guid currentId = queue.Dequeue();

                if (workById.TryGetValue(currentId, out WorkScheduleStageWork? current)
                    && predsBySuccessor.TryGetValue(currentId, out List<WorkDependencyDto>? preds))
                {
                    int maxShift = 0;
                    foreach (WorkDependencyDto dep in preds)
                    {
                        if (!workById.TryGetValue(dep.PredecessorWorkId, out WorkScheduleStageWork? pred))
                            continue;

                        // pred.Periods already reflects any shift applied in an earlier iteration
                        int shift = ComputeRequiredShift(pred.Periods, current.Periods, dep.DependencyType, dep.LagDays);
                        maxShift = Math.Max(maxShift, shift);
                    }

                    if (maxShift > 0)
                    {
                        List<WorkScheduleStageWorkPeriod> periods = current.Periods.ToList();
                        foreach (WorkScheduleStageWorkPeriod period in periods)
                        {
                            period.StartDate = period.StartDate.AddDays(maxShift);
                            period.EndDate = period.EndDate.AddDays(maxShift);
                        }

                        await periodRepository.UpdateRange(periods);

                        current.PlannedStartDate = periods.Min(p => p.StartDate);
                        current.PlannedEndDate = periods.Max(p => p.EndDate);
                        current.UpdatedAt = DateTime.UtcNow;

                        await workRepository.Update(current);
                    }
                }

                if (successorsByPred.TryGetValue(currentId, out List<Guid>? successorIds))
                    foreach (Guid succId in successorIds)
                        if (--inDegree[succId] == 0)
                            queue.Enqueue(succId);
            }
        }

        private static int ComputeRequiredShift(
            ICollection<WorkScheduleStageWorkPeriod> predPeriods,
            ICollection<WorkScheduleStageWorkPeriod> succPeriods,
            WorkDependencyType dependencyType,
            int lagDays)
        {
            if (predPeriods.Count == 0 || succPeriods.Count == 0)
                return 0;

            DateTime predMinStart = predPeriods.Min(p => p.StartDate);
            DateTime predMaxEnd = predPeriods.Max(p => p.EndDate);
            DateTime succMinStart = succPeriods.Min(p => p.StartDate);
            DateTime succMaxEnd = succPeriods.Max(p => p.EndDate);

            TimeSpan delta = dependencyType switch
            {
                WorkDependencyType.FinishToStart => predMaxEnd.AddDays(lagDays) - succMinStart,
                WorkDependencyType.StartToStart => predMinStart.AddDays(lagDays) - succMinStart,
                WorkDependencyType.FinishToFinish => predMaxEnd.AddDays(lagDays) - succMaxEnd,
                WorkDependencyType.StartToFinish => predMinStart.AddDays(lagDays) - succMaxEnd,
                _ => TimeSpan.Zero
            };

            return Math.Max(0, (int)Math.Ceiling(delta.TotalDays));
        }

        private static void ValidateAllDependencyIdsExist(
            List<WorkDependencyDto> dependencies,
            HashSet<Guid> workIds)
        {
            foreach (WorkDependencyDto dto in dependencies)
            {
                if (!workIds.Contains(dto.PredecessorWorkId))
                {
                    throw new ValidationApiException(
                        $"PredecessorWorkId '{dto.PredecessorWorkId}' does not belong to this work schedule.");
                }

                if (!workIds.Contains(dto.SuccessorWorkId))
                {
                    throw new ValidationApiException(
                        $"SuccessorWorkId '{dto.SuccessorWorkId}' does not belong to this work schedule.");
                }
            }
        }
    }
}
