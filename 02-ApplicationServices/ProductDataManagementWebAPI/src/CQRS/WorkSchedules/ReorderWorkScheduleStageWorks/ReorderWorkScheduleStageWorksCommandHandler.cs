using Business.Interfaces.Services;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed class ReorderWorkScheduleStageWorksCommandHandler : IRequestHandler<ReorderWorkScheduleStageWorksCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public ReorderWorkScheduleStageWorksCommandHandler(
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepo = workRepo;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(ReorderWorkScheduleStageWorksCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            IEnumerable<WorkScheduleStageWork> worksRaw = await workRepo.GetBySearch(
                w => w.WorkScheduleStageId == request.WorkScheduleStageId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId);

            Dictionary<Guid, WorkScheduleStageWork> workMap = worksRaw.ToDictionary(w => w.Id);

            List<WorkScheduleStageWork> worksToUpdate = WorkScheduleOrderHelper.ReassignSequentialOrders(
                request.OrderedWorkIds,
                workMap,
                static (w, i) => w.Order = i,
                "OrderedWorkIds must contain exactly all works from the stage.");

            await workRepo.UpdateRange(worksToUpdate);
            await workRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
