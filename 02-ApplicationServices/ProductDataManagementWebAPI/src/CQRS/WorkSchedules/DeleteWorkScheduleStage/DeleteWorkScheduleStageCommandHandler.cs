using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostTrackers;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStage
{
    public sealed class DeleteWorkScheduleStageCommandHandler : IRequestHandler<DeleteWorkScheduleStageCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public DeleteWorkScheduleStageCommandHandler(
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IRepository<TrackedCost> trackedCostRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.dependencyRepo = dependencyRepo;
            this.trackedCostRepository = trackedCostRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleStageCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStage> allScheduleStages = await stageRepo.GetBySearch(
                s => s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId);

            List<WorkScheduleStage> allStagesList = allScheduleStages.ToList();

            WorkScheduleStage? targetStage = allStagesList.FirstOrDefault(s => s.Id == request.StageId);

            if (targetStage == null)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStage), request.StageId.ToString());
            }

            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            List<Guid> stageIdsInSubtree = CollectSubtreeIds(allStagesList, request.StageId);

            List<Guid> workIds = await workRepo.SelectAsync(
                w => stageIdsInSubtree.Contains(w.WorkScheduleStageId),
                w => w.Id,
                cancellationToken);

            if (workIds.Count > 0)
            {
                // Nulluj TrackedCost.WorkScheduleStageWorkId przed soft-delete
                await trackedCostRepository.ExecuteUpdateAsync(
                    x => workIds.Contains(x.WorkScheduleStageWorkId!.Value),
                    x => x.SetProperty(p => p.WorkScheduleStageWorkId, (Guid?)null),
                    cancellationToken);

                // Nulluj WorkScheduleStageWork.CostEstimateItemId
                await workRepo.ExecuteUpdateAsync(
                    x => workIds.Contains(x.Id),
                    x => x.SetProperty(p => p.CostEstimateItemId, (Guid?)null),
                    cancellationToken);
            }

            // Nulluj WorkScheduleStage.CostEstimateGroupId
            await stageRepo.ExecuteUpdateAsync(
                x => stageIdsInSubtree.Contains(x.Id),
                x => x.SetProperty(p => p.CostEstimateGroupId, (Guid?)null),
                cancellationToken);

            if (workIds.Count > 0)
            {
                await dependencyRepo.ExecuteDeleteAsync(
                    d => workIds.Contains(d.PredecessorWorkId) || workIds.Contains(d.SuccessorWorkId),
                    cancellationToken);

                // Soft-delete WorkScheduleStageWorks
                await workRepo.ExecuteUpdateAsync(
                    x => workIds.Contains(x.Id),
                    x => x.SetProperty(p => p.IsDeleted, true)
                          .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
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
