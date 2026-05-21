using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, Guid>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IWorkScheduleSyncService workScheduleSyncService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
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
                CostEstimateId = request.CostEstimateId,
                CreatedByUserId = currentUser.Id
            };

            await workScheduleRepo.Insert(workSchedule);
            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            if (request.CostEstimateId.HasValue)
            {
                await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
            }

            return workSchedule.Id;
        }
    }
}
