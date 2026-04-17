using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.RenameWorkScheduleStage
{
    public sealed class RenameWorkScheduleStageCommandHandler : IRequestHandler<RenameWorkScheduleStageCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IWorkScheduleCacheService scheduleCache;

        public RenameWorkScheduleStageCommandHandler(
            IRepository<WorkScheduleStage> stageRepo,
            IWorkScheduleCacheService scheduleCache)
        {
            this.stageRepo = stageRepo;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(RenameWorkScheduleStageCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStage stage = await stageRepo.GetFirstBySearch(
                s => s.Id == request.StageId
                  && s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStage), request.StageId.ToString());

            stage.Name = request.Name;

            await stageRepo.Update(stage);
            await stageRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
