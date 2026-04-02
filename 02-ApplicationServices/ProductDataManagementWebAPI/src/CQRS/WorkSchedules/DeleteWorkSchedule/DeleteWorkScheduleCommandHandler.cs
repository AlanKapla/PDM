using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public sealed class DeleteWorkScheduleCommandHandler : IRequestHandler<DeleteWorkScheduleCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly ICurrentUser currentUser;

        public DeleteWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            if (currentUser.ActiveTenantId != tenantId)
            {
                throw new ForbiddenApiException("Access to this tenant is not allowed.");
            }

            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId
                      && ws.TenantId == tenantId
                      && ws.ProjectId == projectId)
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);

            if (!isAdmin && workSchedule.CreatedByUserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Only the owner or an admin can delete this work schedule.");
            }

            workSchedule.IsDeleted = true;
            workSchedule.DeletedAt = DateTime.UtcNow;
            await workScheduleRepo.Update(workSchedule);

            return Unit.Value;
        }
    }
}
