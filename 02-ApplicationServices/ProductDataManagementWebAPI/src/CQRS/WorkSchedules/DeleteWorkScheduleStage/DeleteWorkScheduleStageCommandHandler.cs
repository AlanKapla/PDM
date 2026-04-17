using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStage
{
    public sealed class DeleteWorkScheduleStageCommandHandler : IRequestHandler<DeleteWorkScheduleStageCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IWorkScheduleCacheService scheduleCache;

        public DeleteWorkScheduleStageCommandHandler(
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IWorkScheduleCacheService scheduleCache)
        {
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.dependencyRepo = dependencyRepo;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleStageCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStage> allScheduleStages = await stageRepo.GetBySearch(
                s => s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted);

            List<WorkScheduleStage> allStagesList = allScheduleStages.ToList();

            WorkScheduleStage? targetStage = allStagesList.FirstOrDefault(s => s.Id == request.StageId);

            if (targetStage == null)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStage), request.StageId.ToString());
            }

            List<Guid> stageIdsInSubtree = CollectSubtreeIds(allStagesList, request.StageId);

            List<Guid> workIds = await workRepo.SelectAsync(
                w => stageIdsInSubtree.Contains(w.WorkScheduleStageId),
                w => w.Id,
                cancellationToken);

            if (workIds.Count > 0)
            {
                await dependencyRepo.ExecuteDeleteAsync(
                    d => workIds.Contains(d.PredecessorWorkId) || workIds.Contains(d.SuccessorWorkId),
                    cancellationToken);

                await workRepo.ExecuteDeleteAsync(
                    w => stageIdsInSubtree.Contains(w.WorkScheduleStageId),
                    cancellationToken);
            }

            DateTime now = DateTime.UtcNow;

            List<WorkScheduleStage> stagesToSoftDelete = allStagesList
                .Where(s => stageIdsInSubtree.Contains(s.Id))
                .ToList();

            foreach (WorkScheduleStage stage in stagesToSoftDelete)
            {
                stage.IsDeleted = true;
                stage.DeletedAt = now;
            }

            await stageRepo.UpdateRange(stagesToSoftDelete);
            await stageRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }

        private static List<Guid> CollectSubtreeIds(List<WorkScheduleStage> allStages, Guid rootId)
        {
            List<Guid> result = new List<Guid> { rootId };

            List<WorkScheduleStage> children = allStages
                .Where(s => s.ParentStageId == rootId)
                .ToList();

            foreach (WorkScheduleStage child in children)
            {
                result.AddRange(CollectSubtreeIds(allStages, child.Id));
            }

            return result;
        }
    }
}
