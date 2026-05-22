using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Handler for soft-deleting a cost estimate and physically removing its share entries.
    /// </summary>
    public sealed class DeleteCostEstimateCommandHandler : IRequestHandler<DeleteCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IRepository<WorkSchedule> workScheduleRepository;
        private readonly IRepository<WorkScheduleStage> stageRepository;
        private readonly IRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public DeleteCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<SharedCostEstimate> sharedCeRepository,
            IRepository<WorkSchedule> workScheduleRepository,
            IRepository<WorkScheduleStage> stageRepository,
            IRepository<WorkScheduleStageWork> stageWorkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.sharedCeRepository = sharedCeRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.stageRepository = stageRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteCostEstimateCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId &&
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel != CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("Only the owner or an admin can delete this cost estimate.");
            }

            // Pobierz IDs należące do tego konkretnego kosztorysu
            List<Guid> allGroupIds = await groupRepository.SelectAsync(
                x => x.CostEstimateId == request.CostEstimateId,
                x => x.Id,
                cancellationToken);

            List<Guid> allItemIds = await itemRepository.SelectAsync(
                x => x.CostEstimateId == request.CostEstimateId,
                x => x.Id,
                cancellationToken);

            // Nulluj FK — tylko rekordy powiązane z tym kosztorysem
            if (allItemIds.Count > 0)
            {
                await trackedCostRepository.ExecuteUpdateAsync(
                    tc => allItemIds.Contains(tc.CostEstimateItemId!.Value),
                    s => s.SetProperty(tc => tc.CostEstimateItemId, (Guid?)null),
                    cancellationToken);
            }

            await workScheduleRepository.ExecuteUpdateAsync(
                ws => ws.CostEstimateId == request.CostEstimateId,
                s => s.SetProperty(ws => ws.CostEstimateId, (Guid?)null),
                cancellationToken);

            if (allGroupIds.Count > 0)
            {
                await stageRepository.ExecuteUpdateAsync(
                    s => allGroupIds.Contains(s.CostEstimateGroupId!.Value),
                    s => s.SetProperty(st => st.CostEstimateGroupId, (Guid?)null),
                    cancellationToken);
            }

            if (allItemIds.Count > 0)
            {
                await stageWorkRepository.ExecuteUpdateAsync(
                    w => allItemIds.Contains(w.CostEstimateItemId!.Value),
                    s => s.SetProperty(w => w.CostEstimateItemId, (Guid?)null),
                    cancellationToken);
            }

            // Soft-delete CostEstimateItems i CostEstimateGroups
            if (allItemIds.Count > 0)
            {
                await itemRepository.ExecuteUpdateAsync(
                    x => allItemIds.Contains(x.Id),
                    x => x.SetProperty(p => p.IsDeleted, true)
                          .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
                    cancellationToken);
            }

            if (allGroupIds.Count > 0)
            {
                await groupRepository.ExecuteUpdateAsync(
                    x => allGroupIds.Contains(x.Id),
                    x => x.SetProperty(p => p.IsDeleted, true)
                          .SetProperty(p => p.DeletedAt, DateTime.UtcNow),
                    cancellationToken);
            }

            costEstimate.IsDeleted = true;
            costEstimate.DeletedAt = DateTime.UtcNow;
            await costEstimateRepository.Update(costEstimate);

            await sharedCeRepository.ExecuteDeleteAsync(
                s => s.CostEstimateId == request.CostEstimateId,
                cancellationToken);

            await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
                request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            await ceAccessService.InvalidateAccessCacheAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
