using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, Guid>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IWorkItemLinkService workItemLinkService;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IWorkItemLinkService workItemLinkService,
            IWorkScheduleSyncService workScheduleSyncService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workItemLinkService = workItemLinkService;
            this.workScheduleSyncService = workScheduleSyncService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            WorkSchedule workSchedule = new WorkSchedule
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id
            };

            await workScheduleRepo.Insert(workSchedule);
            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            await workItemLinkService.CreateWorkScheduleLinkAsync(
                workSchedule.Id, request.CostEstimateId, cancellationToken);

            if (request.CostEstimateId.HasValue)
            {
                await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
            }

            return workSchedule.Id;
        }
    }
}
