using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed class AddWorkScheduleStageWorkCommandHandler : IRequestHandler<AddWorkScheduleStageWorkCommand, Guid>
    {
        private readonly IRepository<WorkScheduleStage> stageRepository;
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public AddWorkScheduleStageWorkCommandHandler(
            IRepository<WorkScheduleStage> stageRepository,
            IRepository<WorkScheduleStageWork> workRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.stageRepository = stageRepository;
            this.workRepository = workRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Guid> Handle(AddWorkScheduleStageWorkCommand request, CancellationToken cancellationToken)
        {
            bool stageExists = await stageRepository.AnyAsync(
                s => s.Id == request.WorkScheduleStageId
                  && s.WorkScheduleId == request.WorkScheduleId
                  && s.TenantId == request.TenantId
                  && !s.IsDeleted,
                cancellationToken);

            if (!stageExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStage), request.WorkScheduleStageId.ToString());
            }

            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            WorkScheduleStageWork work = new WorkScheduleStageWork
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                WorkScheduleStageId = request.WorkScheduleStageId,
                CostEstimateItemId = request.CostEstimateItemId,
                Name = request.Name,
                Order = request.Order,
                ColorRgb = request.ColorRgb
            };

            await workRepository.Insert(work);
            await workRepository.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return work.Id;
        }
    }
}
