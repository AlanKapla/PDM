using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly WorkScheduleBuilder scheduleBuilder;
        private readonly IWorkScheduleAccessService accessService;

        public SetWorkScheduleDependenciesCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepository,
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IUserService userService,
            IWorkScheduleCacheService scheduleCache,
            WorkScheduleBuilder scheduleBuilder,
            IWorkScheduleAccessService accessService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workRepository = workRepository;
            this.dependencyRepository = dependencyRepository;
            this.periodRepository = periodRepository;
            this.userService = userService;
            this.scheduleCache = scheduleCache;
            this.scheduleBuilder = scheduleBuilder;
            this.accessService = accessService;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(SetWorkScheduleDependenciesCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

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

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return await scheduleBuilder.BuildAsync(request.WorkScheduleId, request.TenantId, request.ProjectId, cancellationToken);
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
