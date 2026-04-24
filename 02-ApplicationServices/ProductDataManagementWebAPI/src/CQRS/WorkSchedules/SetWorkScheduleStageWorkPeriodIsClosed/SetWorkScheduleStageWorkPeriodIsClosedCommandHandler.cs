using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandHandler : IRequestHandler<SetWorkScheduleStageWorkPeriodIsClosedCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;
        private readonly IWorkItemLinkService workItemLinkService;

        public SetWorkScheduleStageWorkPeriodIsClosedCommandHandler(
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IRepository<WorkScheduleStageWork> workRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService,
            IWorkItemLinkService workItemLinkService)
        {
            this.periodRepository = periodRepository;
            this.workRepository = workRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
            this.workItemLinkService = workItemLinkService;
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

            WorkScheduleStageWork? work = await workRepository.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId);

            if (work != null)
            {
                IEnumerable<WorkScheduleStageWorkPeriod> allPeriods = await periodRepository.GetBySearch(
                    p => p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId);
                bool allClosed = allPeriods.All(p => p.IsClosed);

                await workItemLinkService.SyncPlannedDatesForStageWorkAsync(
                    request.WorkScheduleStageWorkId, work.PlannedStartDate, work.PlannedEndDate, allClosed, cancellationToken);
            }

            await scheduleCache.InvalidateWorkAsync(request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);
            return Unit.Value;
        }
    }
}
