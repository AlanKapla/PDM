using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public sealed class DeleteWorkScheduleCommandHandler : IRequestHandler<DeleteWorkScheduleCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;
        private readonly IWorkItemLinkService workItemLinkService;

        public DeleteWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService,
            IWorkItemLinkService workItemLinkService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
            this.workItemLinkService = workItemLinkService;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId
                   && ws.TenantId == request.TenantId
                   && ws.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            workSchedule.IsDeleted = true;
            workSchedule.DeletedAt = DateTime.UtcNow;
            await workScheduleRepo.Update(workSchedule);

            await workItemLinkService.DeleteAllLinksForScheduleAsync(request.WorkScheduleId, cancellationToken);

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return Unit.Value;
        }
    }
}
