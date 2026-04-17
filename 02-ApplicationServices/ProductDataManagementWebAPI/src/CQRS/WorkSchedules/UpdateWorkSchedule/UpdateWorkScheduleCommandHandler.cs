using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandHandler : IRequestHandler<UpdateWorkScheduleCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;

        public UpdateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId
                   && ws.TenantId == request.TenantId
                   && ws.ProjectId == request.ProjectId
                   && !ws.IsDeleted);

            if (workSchedule is null)
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);

            if (!isAdmin && workSchedule.CreatedByUserId != currentUser.Id)
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            workSchedule.Name = request.Name;

            await workScheduleRepo.Update(workSchedule);
            await workScheduleRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return Unit.Value;
        }
    }
}
