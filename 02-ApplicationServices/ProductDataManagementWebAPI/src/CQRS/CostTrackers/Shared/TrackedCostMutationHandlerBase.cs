using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.CostTrackers;
using Entities.Models.WorkItemLinks;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class TrackedCostMutationHandlerBase : CostTrackerHandlerBase
    {
        private readonly IReadRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository;
        protected readonly ICostTrackerAttachmentService attachmentService;

        protected TrackedCostMutationHandlerBase(
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository,
            ICostTrackerAttachmentService attachmentService)
            : base(currentUser, trackedCostRepository, attachmentService)
        {
            this.workItemLinkRepository = workItemLinkRepository;
            this.attachmentService = attachmentService;
        }

        protected async Task ValidateWorkItemLinkAsync(
            Guid? workItemLinkId, Guid projectId, CancellationToken cancellationToken)
        {
            if (!workItemLinkId.HasValue)
                return;

            bool exists = await workItemLinkRepository.AnyAsync(
                l => l.Id == workItemLinkId.Value && l.ProjectId == projectId,
                cancellationToken);

            if (!exists)
                throw new NotFoundApiException(nameof(CostEstimateItemWorkScheduleStageWorkLink), workItemLinkId.Value.ToString());
        }

        protected TrackedCostWeb BuildCostWeb(TrackedCost cost, IEnumerable<TrackedCostAttachment> attachments)
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
                WorkItemLinkId = cost.WorkItemLinkId,
                CostEstimateItemId = cost.CostEstimateItemId ?? cost.CostEstimateItemWorkScheduleStageWorkLink?.CostEstimateItemId,
                WorkScheduleStageWorkId = cost.WorkScheduleStageWorkId ?? cost.CostEstimateItemWorkScheduleStageWorkLink?.WorkScheduleStageWorkId,
                IsAdditional = !cost.WorkItemLinkId.HasValue,
                SourceType = ResolveSourceType(cost),
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

