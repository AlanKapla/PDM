using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed class ReorderWorkScheduleStageWorksCommandHandler : IRequestHandler<ReorderWorkScheduleStageWorksCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleCacheService scheduleCache;

        public ReorderWorkScheduleStageWorksCommandHandler(
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workRepo = workRepo;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(ReorderWorkScheduleStageWorksCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStageWork> worksRaw = await workRepo.GetBySearch(
                w => w.WorkScheduleStageId == request.WorkScheduleStageId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId);

            Dictionary<Guid, WorkScheduleStageWork> workMap = worksRaw.ToDictionary(w => w.Id);

            foreach (Guid id in request.OrderedWorkIds)
            {
                if (!workMap.ContainsKey(id))
                    throw new ValidationApiException($"Work {id} does not belong to stage {request.WorkScheduleStageId}.");
            }

            for (int i = 0; i < request.OrderedWorkIds.Count; i++)
            {
                workMap[request.OrderedWorkIds[i]].Order = i;
            }

            List<WorkScheduleStageWork> worksToUpdate = request.OrderedWorkIds
                .Select(id => workMap[id])
                .ToList();

            await workRepo.UpdateRange(worksToUpdate);
            await workRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
