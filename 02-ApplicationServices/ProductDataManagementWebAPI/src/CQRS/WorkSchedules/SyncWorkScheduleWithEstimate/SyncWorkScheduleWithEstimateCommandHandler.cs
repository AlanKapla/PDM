using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.WorkItemLinks;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SyncWorkScheduleWithEstimate
{
    public class SyncWorkScheduleWithEstimateCommandHandler : IRequestHandler<SyncWorkScheduleWithEstimateCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepo;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICostEstimateAccessService costEstimateAccessService;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;

        public SyncWorkScheduleWithEstimateCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepo,
            IWorkScheduleSyncService workScheduleSyncService,
            ICostEstimateAccessService costEstimateAccessService,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workScheduleLinkRepo = workScheduleLinkRepo;
            this.workScheduleSyncService = workScheduleSyncService;
            this.costEstimateAccessService = costEstimateAccessService;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(SyncWorkScheduleWithEstimateCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            WorkSchedule workSchedule = await LoadAndAuthorizeAsync(tenantId, projectId, request.WorkScheduleId, cancellationToken);

            CostEstimateWorkScheduleLink? link = await workScheduleLinkRepo.GetFirstBySearch(
                l => l.WorkScheduleId == workSchedule.Id);

            if (link?.CostEstimateId == null)
            {
                throw new ValidationApiException("Work schedule is not linked to a cost estimate.");
            }

            CostEstimateAccessLevel accessLevel = await costEstimateAccessService.GetAccessLevelAsync(
                currentUser, tenantId, projectId, link.CostEstimateId.Value, cancellationToken);

            if (accessLevel < CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("You do not have full access to the linked cost estimate.");
            }

            await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return Unit.Value;
        }

        private async Task<WorkSchedule> LoadAndAuthorizeAsync(
            Guid tenantId, Guid projectId, Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId && !ws.IsDeleted)
                ?? throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);

            if (!isAdmin && workSchedule.CreatedByUserId != currentUser.Id)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());
            }

            return workSchedule;
        }
    }
}
