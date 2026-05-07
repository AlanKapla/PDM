using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
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
            bool workExists = await workRepository.AnyAsync(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId,
                cancellationToken);

            if (!workExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());
            }

            await accessService.RequireAdminOwnerOrAssignedAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);

            WorkScheduleStageWork work = await workRepository.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

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

            bool allClosed = periodList.Count > 0 && periodList.All(p => p.IsClosed);

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
