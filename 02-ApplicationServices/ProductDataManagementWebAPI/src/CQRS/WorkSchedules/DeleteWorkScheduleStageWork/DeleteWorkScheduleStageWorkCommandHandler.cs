using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
using Entities.Models.CostTrackers;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWork
{
    public sealed class DeleteWorkScheduleStageWorkCommandHandler : IRequestHandler<DeleteWorkScheduleStageWorkCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public DeleteWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepository = workRepository;
            this.dependencyRepository = dependencyRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleStageWorkCommand request, CancellationToken cancellationToken)
        {
            bool workExists = await workRepository.AnyAsync(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.WorkScheduleStageId == request.WorkScheduleStageId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId,
                cancellationToken);

            if (!workExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());
            }

            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            // Nulluj TrackedCost.WorkScheduleStageWorkId przed soft-delete
            await trackedCostRepository.ExecuteUpdateAsync(
                x => x.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId,
                x => x.SetProperty(p => p.WorkScheduleStageWorkId, (Guid?)null),
                cancellationToken);

            // Nulluj WorkScheduleStageWork.CostEstimateItemId
            await workRepository.ExecuteUpdateAsync(
                x => x.Id == request.WorkScheduleStageWorkId,
                x => x.SetProperty(p => p.CostEstimateItemId, (Guid?)null),
                cancellationToken);

            await dependencyRepository.ExecuteDeleteAsync(
                d => d.PredecessorWorkId == request.WorkScheduleStageWorkId
                  || d.SuccessorWorkId == request.WorkScheduleStageWorkId,
                cancellationToken);

            // Soft-delete WorkScheduleStageWork
            await workRepository.ExecuteUpdateAsync(
                x => x.Id == request.WorkScheduleStageWorkId,
                x => x.SetProperty(p => p.IsDeleted, true)
                      .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
                cancellationToken);

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
