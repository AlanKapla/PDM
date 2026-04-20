using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
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
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted);

            Dictionary<Guid, WorkScheduleStage> stageMap = stagesRaw.ToDictionary(s => s.Id);
            HashSet<Guid> orderedStageIds = request.OrderedStageIds.ToHashSet();

            if (request.OrderedStageIds.Count != stageMap.Count
                || orderedStageIds.Count != stageMap.Count
                || !orderedStageIds.SetEquals(stageMap.Keys))
            {
                throw new ValidationApiException("OrderedStageIds must contain exactly all stages from the work schedule.");
            }

            for (int i = 0; i < request.OrderedStageIds.Count; i++)
            {
                stageMap[request.OrderedStageIds[i]].Order = i;
            }

            List<WorkScheduleStage> stagesToUpdate = request.OrderedStageIds
                .Select(id => stageMap[id])
                .ToList();

            await stageRepo.UpdateRange(stagesToUpdate);
            await stageRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
