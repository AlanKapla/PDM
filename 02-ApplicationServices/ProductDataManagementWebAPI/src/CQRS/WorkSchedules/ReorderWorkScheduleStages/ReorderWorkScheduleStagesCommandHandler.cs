using Business.Interfaces.Exceptions;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStages
{
    public sealed class ReorderWorkScheduleStagesCommandHandler : IRequestHandler<ReorderWorkScheduleStagesCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;

        public ReorderWorkScheduleStagesCommandHandler(IRepository<WorkScheduleStage> stageRepo)
        {
            this.stageRepo = stageRepo;
        }

        public async Task<Unit> Handle(ReorderWorkScheduleStagesCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStage> stagesRaw = await stageRepo.GetBySearch(
                s => s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted);

            Dictionary<Guid, WorkScheduleStage> stageMap = stagesRaw.ToDictionary(s => s.Id);

            foreach (Guid id in request.OrderedStageIds)
            {
                if (!stageMap.ContainsKey(id))
                    throw new ValidationApiException($"Stage {id} does not belong to work schedule {request.WorkScheduleId}.");
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
            return Unit.Value;
        }
    }
}
