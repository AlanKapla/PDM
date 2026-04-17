using Business.Interfaces.Exceptions;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWork
{
    public sealed class DeleteWorkScheduleStageWorkCommandHandler : IRequestHandler<DeleteWorkScheduleStageWorkCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepository;

        public DeleteWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepository)
        {
            this.workRepository = workRepository;
            this.dependencyRepository = dependencyRepository;
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

            await dependencyRepository.ExecuteDeleteAsync(
                d => d.PredecessorWorkId == request.WorkScheduleStageWorkId
                  || d.SuccessorWorkId == request.WorkScheduleStageWorkId,
                cancellationToken);

            await workRepository.ExecuteDeleteAsync(
                w => w.Id == request.WorkScheduleStageWorkId,
                cancellationToken);

            return Unit.Value;
        }
    }
}
