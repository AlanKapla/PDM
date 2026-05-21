using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandHandler : IRequestHandler<SetWorkScheduleStageWorkPeriodIsClosedCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public SetWorkScheduleStageWorkPeriodIsClosedCommandHandler(
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.periodRepository = periodRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkPeriodIsClosedCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOwnerOrAssignedAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);

            WorkScheduleStageWorkPeriod period = await periodRepository.GetFirstBySearch(
                p => p.Id == request.PeriodId
                  && p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && p.TenantId == request.TenantId
                  && p.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWorkPeriod), request.PeriodId.ToString());

            period.IsClosed = request.IsClosed;

            await periodRepository.Update(period);
            await periodRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
