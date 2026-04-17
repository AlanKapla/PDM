using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.RenameWorkScheduleStageWork
{
    public sealed class RenameWorkScheduleStageWorkCommandHandler : IRequestHandler<RenameWorkScheduleStageWorkCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleCacheService scheduleCache;

        public RenameWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workRepo = workRepo;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(RenameWorkScheduleStageWorkCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWork work = await workRepo.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.WorkScheduleStageId == request.WorkScheduleStageId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

            work.Name = request.Name;

            await workRepo.Update(work);
            await workRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
