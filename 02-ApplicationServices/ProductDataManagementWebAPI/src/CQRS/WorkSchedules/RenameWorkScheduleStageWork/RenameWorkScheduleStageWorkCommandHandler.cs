using Business.Interfaces.Exceptions;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.RenameWorkScheduleStageWork
{
    public sealed class RenameWorkScheduleStageWorkCommandHandler : IRequestHandler<RenameWorkScheduleStageWorkCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepo;

        public RenameWorkScheduleStageWorkCommandHandler(IRepository<WorkScheduleStageWork> workRepo)
        {
            this.workRepo = workRepo;
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
            return Unit.Value;
        }
    }
}
