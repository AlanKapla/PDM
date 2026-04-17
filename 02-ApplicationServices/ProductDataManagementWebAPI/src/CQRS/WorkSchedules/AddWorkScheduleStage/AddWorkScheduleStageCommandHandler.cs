using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.AddWorkScheduleStage
{
    public sealed class AddWorkScheduleStageCommandHandler : IRequestHandler<AddWorkScheduleStageCommand, Guid>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IWorkScheduleCacheService scheduleCache;

        public AddWorkScheduleStageCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Guid> Handle(AddWorkScheduleStageCommand request, CancellationToken cancellationToken)
        {
            bool scheduleExists = await workScheduleRepo.AnyAsync(
                ws => ws.Id == request.WorkScheduleId
                   && ws.TenantId == request.TenantId
                   && ws.ProjectId == request.ProjectId
                   && !ws.IsDeleted,
                cancellationToken);

            if (!scheduleExists)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
            }

            WorkScheduleStage stage = new WorkScheduleStage
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                WorkScheduleId = request.WorkScheduleId,
                ParentStageId = request.ParentStageId,
                CostEstimateGroupId = request.CostEstimateGroupId,
                Name = request.Name,
                Order = request.Order
            };

            await stageRepo.Insert(stage);
            await stageRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return stage.Id;
        }
    }
}
