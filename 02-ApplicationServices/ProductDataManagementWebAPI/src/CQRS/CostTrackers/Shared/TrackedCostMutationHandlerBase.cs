using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class TrackedCostMutationHandlerBase : CostTrackerHandlerBase
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<CostEstimateItem> itemRepository;
        protected readonly ICostTrackerAttachmentService attachmentService;

        protected TrackedCostMutationHandlerBase(
            IReadRepository<CostTracker> trackerRepository,
            IReadRepository<Project> projectRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateItem> itemRepository,
            ICostTrackerAttachmentService attachmentService,
            IReadRepository<TrackedCost> trackedCostRepository,
            ICurrentUser currentUser)
            : base(trackerRepository, currentUser, trackedCostRepository, attachmentService)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.itemRepository = itemRepository;
            this.attachmentService = attachmentService;
        }

        protected async Task ValidateCostEstimateAndItemAsync(
            Guid? costEstimateId, Guid trackerProjectId, Guid? itemId, CancellationToken cancellationToken)
        {
            if (costEstimateId.HasValue)
            {
                await ValidateCostEstimateAsync(costEstimateId.Value, trackerProjectId, cancellationToken);
            }

            if (itemId.HasValue)
            {
                await ValidateCostEstimateItemAsync(itemId.Value, costEstimateId, cancellationToken);
            }
        }

        private async Task ValidateCostEstimateAsync(
            Guid costEstimateId, Guid trackerProjectId, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await costEstimateRepository.GetFirstBySearch(
                ce => ce.Id == costEstimateId && ce.ProjectId == trackerProjectId && !ce.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimate), costEstimateId.ToString());
        }

        private async Task ValidateCostEstimateItemAsync(
            Guid itemId, Guid? estimateId, CancellationToken cancellationToken)
        {
            if (!estimateId.HasValue)
            {
                throw new ValidationApiException("CostEstimateId is required when CostEstimateItemId is provided.");
            }

            CostEstimateItem item = await itemRepository.GetFirstBySearch(
                i => i.Id == itemId && i.CostEstimateId == estimateId.Value && i.RelationType == ItemRelationType.None && !i.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimateItem), itemId.ToString());
        }

        protected TrackedCostWeb BuildCostWeb(TrackedCost cost, IEnumerable<TrackedCostAttachment> attachments)
        {
            var attachmentWebs = attachments
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
                TrackerId = cost.TrackerId,
                CostEstimateId = cost.CostEstimateId,
                CostEstimateItemId = cost.CostEstimateItemId,
                IsAdditional = !cost.CostEstimateItemId.HasValue,
                Name = cost.Name,
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
