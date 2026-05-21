using Business.Interfaces.Services;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStages
{
    public sealed class ReorderWorkScheduleStagesCommandHandler : IRequestHandler<ReorderWorkScheduleStagesCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public ReorderWorkScheduleStagesCommandHandler(
            IRepository<WorkScheduleStage> stageRepo,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.stageRepo = stageRepo;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(ReorderWorkScheduleStagesCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            IEnumerable<WorkScheduleStage> stagesRaw = await stageRepo.GetBySearch(
                s => s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId);

            Dictionary<Guid, WorkScheduleStage> stageMap = stagesRaw.ToDictionary(s => s.Id);

            List<WorkScheduleStage> stagesToUpdate = WorkScheduleOrderHelper.ReassignSequentialOrders(
                request.OrderedStageIds,
                stageMap,
                static (s, i) => s.Order = i,
                "OrderedStageIds must contain exactly all stages from the work schedule.");

            await stageRepo.UpdateRange(stagesToUpdate);
            await stageRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
