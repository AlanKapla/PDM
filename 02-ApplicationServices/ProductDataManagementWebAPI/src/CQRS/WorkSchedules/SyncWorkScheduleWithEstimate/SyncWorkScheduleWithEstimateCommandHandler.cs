using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SyncWorkScheduleWithEstimate
{
    public sealed class SyncWorkScheduleWithEstimateCommandHandler : IRequestHandler<SyncWorkScheduleWithEstimateCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICostEstimateAccessService costEstimateAccessService;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public SyncWorkScheduleWithEstimateCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IWorkScheduleSyncService workScheduleSyncService,
            ICostEstimateAccessService costEstimateAccessService,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workScheduleSyncService = workScheduleSyncService;
            this.costEstimateAccessService = costEstimateAccessService;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(SyncWorkScheduleWithEstimateCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            await accessService.RequireAdminOrOwnerAsync(tenantId, projectId, request.WorkScheduleId, cancellationToken);

            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId)
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            if (!workSchedule.CostEstimateId.HasValue)
            {
                throw new ValidationApiException("Work schedule is not linked to a cost estimate.");
            }

            CostEstimateAccessLevel accessLevel = await costEstimateAccessService.GetAccessLevelAsync(
                currentUser, tenantId, projectId, workSchedule.CostEstimateId.Value, cancellationToken);

            if (accessLevel < CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("You do not have full access to the linked cost estimate.");
            }

            await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return Unit.Value;
        }
    }
}
