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

namespace CQRS.WorkSchedules.MoveWorkScheduleStageWork
{
    public sealed class MoveWorkScheduleStageWorkCommandHandler : IRequestHandler<MoveWorkScheduleStageWorkCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public MoveWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(MoveWorkScheduleStageWorkCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            bool targetStageExists = await stageRepo.AnyAsync(
                s => s.Id == request.TargetStageId
                  && s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId,
                cancellationToken);

            if (!targetStageExists)
                throw new ValidationApiException($"Target stage {request.TargetStageId} does not belong to work schedule {request.WorkScheduleId}.");

            WorkScheduleStageWork work = await workRepo.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

            IEnumerable<WorkScheduleStageWork> targetWorksRaw = await workRepo.GetBySearch(
                w => w.WorkScheduleStageId == request.TargetStageId
                  && w.Id != request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId);

            List<WorkScheduleStageWork> worksToShift;
            bool isSameStageMove = work.WorkScheduleStageId == request.TargetStageId;
            if (isSameStageMove && request.TargetOrder == work.Order)
            {
                worksToShift = new List<WorkScheduleStageWork>();
            }
            else if (isSameStageMove && request.TargetOrder > work.Order)
            {
                worksToShift = targetWorksRaw
                    .Where(w => w.Order > work.Order && w.Order <= request.TargetOrder)
                    .ToList();

                foreach (WorkScheduleStageWork w in worksToShift)
                {
                    w.Order--;
                }
            }
            else
            {
                worksToShift = targetWorksRaw
                    .Where(w => w.Order >= request.TargetOrder)
                    .ToList();

                foreach (WorkScheduleStageWork w in worksToShift)
                {
                    w.Order++;
                }
            }

            work.WorkScheduleStageId = request.TargetStageId;
            work.Order = request.TargetOrder;

            if (worksToShift.Count > 0)
                await workRepo.UpdateRange(worksToShift);

            await workRepo.Update(work);
            await workRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
