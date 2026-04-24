using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostTrackers;
using Entities.Models.WorkItemLinks;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.UpdateTrackedCost
{
    public sealed class UpdateTrackedCostCommandHandler
        : TrackedCostMutationHandlerBase, IRequestHandler<UpdateTrackedCostCommand, TrackedCostWeb>
    {
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly ICostTrackerFinancialService financialService;
        private readonly ILogger<UpdateTrackedCostCommandHandler> logger;

        public UpdateTrackedCostCommandHandler(
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser,
            ILogger<UpdateTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, workItemLinkRepository, attachmentService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.financialService = financialService;
            this.logger = logger;
        }

        public async Task<TrackedCostWeb> Handle(
            UpdateTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            TrackedCost cost = await GetAndValidateTrackedCostAsync(request.CostId, request.TenantId, request.ProjectId, cancellationToken);

            var (net, gross) = financialService.Calculate(request.Net, request.Gross);

            cost.Name = request.Name;
            cost.Number = request.Number;
            cost.Description = request.Description;
            cost.Net = net;
            cost.Gross = gross;
            cost.Contractor = request.Contractor;
            cost.Date = request.Date;
            cost.UpdatedAt = DateTime.UtcNow;

            await trackedCostRepository.Update(cost);

            logger.LogInformation(
                "Updated TrackedCost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            IReadOnlyList<Guid>? effectiveExistingIds = request.ClearAllAttachments
                ? new List<Guid>()
                : request.ExistingAttachmentIds;

            var attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, effectiveExistingIds, request.TenantId, request.ProjectId, cancellationToken);

            return BuildCostWeb(cost, attachments);
        }
    }
}
