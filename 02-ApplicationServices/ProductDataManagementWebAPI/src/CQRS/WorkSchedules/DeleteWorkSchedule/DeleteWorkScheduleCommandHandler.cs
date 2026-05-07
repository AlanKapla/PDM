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
using Entities.Models.CostTrackers;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public sealed class DeleteWorkScheduleCommandHandler : IRequestHandler<DeleteWorkScheduleCommand, Unit>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepository;
        private readonly IRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public DeleteWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepository,
            IRepository<WorkScheduleStageWork> stageWorkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepository = stageRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId
                   && ws.TenantId == request.TenantId
                   && ws.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            List<Guid> allStageIds = await stageRepository.SelectAsync(
                x => x.WorkScheduleId == request.WorkScheduleId
                  && x.TenantId == request.TenantId,
                x => x.Id,
                cancellationToken);

            List<Guid> allStageWorkIds = await stageWorkRepository.SelectAsync(
                x => allStageIds.Contains(x.WorkScheduleStageId)
                  && x.TenantId == request.TenantId,
                x => x.Id,
                cancellationToken);

            if (allStageWorkIds.Count > 0)
            {
                // Nulluj TrackedCost.WorkScheduleStageWorkId
                await trackedCostRepository.ExecuteUpdateAsync(
                    x => allStageWorkIds.Contains(x.WorkScheduleStageWorkId!.Value),
                    x => x.SetProperty(p => p.WorkScheduleStageWorkId, (Guid?)null),
                    cancellationToken);

                // Nulluj WorkScheduleStageWork.CostEstimateItemId
                await stageWorkRepository.ExecuteUpdateAsync(
                    x => allStageWorkIds.Contains(x.Id),
                    x => x.SetProperty(p => p.CostEstimateItemId, (Guid?)null),
                    cancellationToken);
            }

            if (allStageIds.Count > 0)
            {
                // Nulluj WorkScheduleStage.CostEstimateGroupId
                await stageRepository.ExecuteUpdateAsync(
                    x => allStageIds.Contains(x.Id),
                    x => x.SetProperty(p => p.CostEstimateGroupId, (Guid?)null),
                    cancellationToken);
            }

            if (allStageWorkIds.Count > 0)
            {
                // Soft-delete WorkScheduleStageWorks
                await stageWorkRepository.ExecuteUpdateAsync(
                    x => allStageWorkIds.Contains(x.Id),
                    x => x.SetProperty(p => p.IsDeleted, true)
                          .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
                    cancellationToken);
            }

            if (allStageIds.Count > 0)
            {
                // Soft-delete WorkScheduleStages
                await stageRepository.ExecuteUpdateAsync(
                    x => allStageIds.Contains(x.Id),
                    x => x.SetProperty(p => p.IsDeleted, true)
                          .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
                    cancellationToken);
            }

            // Soft-delete WorkSchedule
            workSchedule.IsDeleted = true;
            workSchedule.DeletedAt = DateTime.UtcNow;
            await workScheduleRepo.Update(workSchedule);

            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);

            return Unit.Value;
        }
    }
}
