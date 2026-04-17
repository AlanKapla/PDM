using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed class SetWorkScheduleStageWorkIsClosedCommandHandler : IRequestHandler<SetWorkScheduleStageWorkIsClosedCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;
        private readonly ICurrentUser currentUser;

        public SetWorkScheduleStageWorkIsClosedCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            ICurrentUser currentUser)
        {
            this.workRepository = workRepository;
            this.periodRepository = periodRepository;
            this.assignmentRepository = assignmentRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkIsClosedCommand request, CancellationToken cancellationToken)
        {
            bool workExists = await workRepository.AnyAsync(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId,
                cancellationToken);

            if (!workExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());
            }

            bool isAssigned = await assignmentRepository.AnyAsync(
                a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && a.UserId == currentUser.Id,
                cancellationToken);

            if (!isAssigned)
            {
                throw new ForbiddenApiException("You are not assigned to this work item.");
            }

            IEnumerable<WorkScheduleStageWorkPeriod> periods = await periodRepository.GetBySearch(
                p => p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId);

            List<WorkScheduleStageWorkPeriod> periodList = periods.ToList();

            foreach (WorkScheduleStageWorkPeriod period in periodList)
            {
                period.IsClosed = request.IsClosed;
            }

            if (periodList.Count > 0)
            {
                await periodRepository.UpdateRange(periodList);
                await periodRepository.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
