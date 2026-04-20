using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, Guid>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            WorkSchedule workSchedule = new WorkSchedule
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                CostEstimateId = request.CostEstimateId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id
            };

            await workScheduleRepo.Insert(workSchedule);
            await workScheduleRepo.SaveChangesAsync(cancellationToken);
            return workSchedule.Id;
        }
    }
}
