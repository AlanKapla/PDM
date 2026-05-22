using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class TrackedCostMutationHandlerBase : CostTrackerHandlerBase
    {
        private readonly IReadRepository<CostEstimateItem> costEstimateItemRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;

        protected TrackedCostMutationHandlerBase(
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<CostEstimateItem> costEstimateItemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            ICostTrackerAttachmentService attachmentService,
            IContractorService contractorService)
            : base(currentUser, trackedCostRepository, attachmentService, contractorService)
        {
            this.costEstimateItemRepository = costEstimateItemRepository;
            this.stageWorkRepository = stageWorkRepository;
        }

        protected async Task ValidateTrackedCostLinksAsync(
            Guid? costEstimateItemId, Guid? workScheduleStageWorkId,
            Guid projectId, Guid tenantId, CancellationToken cancellationToken)
        {
            if (costEstimateItemId.HasValue)
            {
                bool itemExists = await costEstimateItemRepository.AnyAsync(
                    i => i.Id == costEstimateItemId.Value
                         && i.CostEstimate.ProjectId == projectId
                         && i.CostEstimate.TenantId == tenantId,
                    cancellationToken);

                if (!itemExists)
                {
                    throw new NotFoundApiException(nameof(CostEstimateItem), costEstimateItemId.Value.ToString());
                }
            }

            if (workScheduleStageWorkId.HasValue)
            {
                bool workExists = await stageWorkRepository.AnyAsync(
                    w => w.Id == workScheduleStageWorkId.Value
                         && w.ProjectId == projectId
                         && w.TenantId == tenantId,
                    cancellationToken);

                if (!workExists)
                {
                    throw new NotFoundApiException(nameof(WorkScheduleStageWork), workScheduleStageWorkId.Value.ToString());
                }
            }

            if (costEstimateItemId.HasValue && workScheduleStageWorkId.HasValue)
            {
                bool linked = await stageWorkRepository.AnyAsync(
                    w => w.Id == workScheduleStageWorkId.Value
                         && w.CostEstimateItemId == costEstimateItemId.Value,
                    cancellationToken);

                if (!linked)
                {
                    throw new ValidationApiException(
                        "WorkScheduleStageWork is not linked to the provided cost estimate item.");
                }
            }
        }
    }
}

