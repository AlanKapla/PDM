using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed class SetWorkScheduleStageWorkIsClosedCommandHandler : IRequestHandler<SetWorkScheduleStageWorkIsClosedCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public SetWorkScheduleStageWorkIsClosedCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepository = workRepository;
            this.periodRepository = periodRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkIsClosedCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWork work = await workRepository.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

            await accessService.RequireAdminOwnerOrAssignedAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);

            IEnumerable<WorkScheduleStageWorkPeriod> periods = await periodRepository.GetBySearch(
                p => p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && p.TenantId == request.TenantId
                  && p.ProjectId == request.ProjectId);

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

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
