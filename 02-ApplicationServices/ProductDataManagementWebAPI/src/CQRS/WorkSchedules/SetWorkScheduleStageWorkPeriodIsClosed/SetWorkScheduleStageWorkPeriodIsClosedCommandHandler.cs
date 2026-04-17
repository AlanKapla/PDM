using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandHandler : IRequestHandler<SetWorkScheduleStageWorkPeriodIsClosedCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;

        public SetWorkScheduleStageWorkPeriodIsClosedCommandHandler(
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache)
        {
            this.periodRepository = periodRepository;
            this.assignmentRepository = assignmentRepository;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkPeriodIsClosedCommand request, CancellationToken cancellationToken)
        {
            bool isAssigned = await assignmentRepository.AnyAsync(
                a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && a.UserId == currentUser.Id,
                cancellationToken);

            if (!isAssigned)
            {
                throw new ForbiddenApiException("You are not assigned to this work item.");
            }

            WorkScheduleStageWorkPeriod period = await periodRepository.GetFirstBySearch(
                p => p.Id == request.PeriodId
                  && p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && p.TenantId == request.TenantId
                  && p.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWorkPeriod), request.PeriodId.ToString());

            period.IsClosed = request.IsClosed;

            await periodRepository.Update(period);
            await periodRepository.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateWorkAsync(request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);
            return Unit.Value;
        }
    }
}
