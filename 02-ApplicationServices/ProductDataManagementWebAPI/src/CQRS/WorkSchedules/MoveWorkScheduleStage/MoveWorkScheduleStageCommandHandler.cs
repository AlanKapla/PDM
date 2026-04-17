using Business.Interfaces.Exceptions;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.MoveWorkScheduleStage
{
    public sealed class MoveWorkScheduleStageCommandHandler : IRequestHandler<MoveWorkScheduleStageCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;

        public MoveWorkScheduleStageCommandHandler(IRepository<WorkScheduleStage> stageRepo)
        {
            this.stageRepo = stageRepo;
        }

        public async Task<Unit> Handle(MoveWorkScheduleStageCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStage> allRaw = await stageRepo.GetBySearch(
                s => s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted);

            List<WorkScheduleStage> allStages = allRaw.ToList();

            WorkScheduleStage stage = allStages.FirstOrDefault(s => s.Id == request.StageId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStage), request.StageId.ToString());

            if (request.ParentStageId.HasValue)
            {
                bool parentExists = allStages.Any(s => s.Id == request.ParentStageId.Value);
                if (!parentExists)
                    throw new ValidationApiException($"Parent stage {request.ParentStageId} does not belong to work schedule {request.WorkScheduleId}.");

                HashSet<Guid> descendants = CollectDescendantIds(allStages, request.StageId);
                if (descendants.Contains(request.ParentStageId.Value))
                    throw new ValidationApiException("Moving a stage under its own descendant would create a cycle in the hierarchy.");
            }

            stage.ParentStageId = request.ParentStageId;

            await stageRepo.Update(stage);
            await stageRepo.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        private static HashSet<Guid> CollectDescendantIds(List<WorkScheduleStage> allStages, Guid rootId)
        {
            HashSet<Guid> result = new HashSet<Guid>();
            Queue<Guid> queue = new Queue<Guid>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                Guid current = queue.Dequeue();
                foreach (WorkScheduleStage child in allStages.Where(s => s.ParentStageId == current))
                {
                    if (result.Add(child.Id))
                        queue.Enqueue(child.Id);
                }
            }

            return result;
        }
    }
}
