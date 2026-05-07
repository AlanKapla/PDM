using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
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
using Entities.Models.Costs;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class TrackedCostMutationHandlerBase : CostTrackerHandlerBase
    {
        private readonly IReadRepository<CostEstimateItem> costEstimateItemRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;
        protected readonly ICostTrackerAttachmentService attachmentService;

        protected TrackedCostMutationHandlerBase(
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<CostEstimateItem> costEstimateItemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            ICostTrackerAttachmentService attachmentService)
            : base(currentUser, trackedCostRepository, attachmentService)
        {
            this.costEstimateItemRepository = costEstimateItemRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.attachmentService = attachmentService;
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
                        "WorkScheduleStageWork nie jest powiązany z podaną pozycją kosztorysu.");
                }
            }
        }

        protected TrackedCostWeb BuildCostWeb(TrackedCost cost, IEnumerable<BaseCostAttachment> attachments)
        {
            List<TrackedCostAttachmentWeb> attachmentWebs = attachments
                .Select(a => new TrackedCostAttachmentWeb
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileUrl = attachmentService.GenerateFileUrl(a),
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            return new TrackedCostWeb
            {
                Id = cost.Id,
                CostEstimateItemId = cost.CostEstimateItemId,
                WorkScheduleStageWorkId = cost.WorkScheduleStageWorkId,
                IsAdditional = !cost.CostEstimateItemId.HasValue && !cost.WorkScheduleStageWorkId.HasValue,
                SourceType = ResolveSourceType(cost),
                Name = cost.Name,
                Number = cost.Number,
                Description = cost.Description,
                Net = cost.Net,
                Gross = cost.Gross,
                Contractor = cost.Contractor,
                Date = cost.Date,
                CreatedAt = cost.CreatedAt,
                UpdatedAt = cost.UpdatedAt,
                Attachments = attachmentWebs
            };
        }
    }
}

