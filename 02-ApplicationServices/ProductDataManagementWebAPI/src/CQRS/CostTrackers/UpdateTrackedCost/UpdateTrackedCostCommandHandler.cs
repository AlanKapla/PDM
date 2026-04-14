using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
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
            IReadRepository<CostTracker> trackerRepository,
            IReadRepository<Project> projectRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateItem> itemRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser,
            ILogger<UpdateTrackedCostCommandHandler> logger)
            : base(trackerRepository, projectRepository, costEstimateRepository, itemRepository, attachmentService, trackedCostRepository, currentUser)
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

            await ValidateCostEstimateAndItemAsync(request.CostEstimateId, request.ProjectId, request.CostEstimateItemId, cancellationToken);

            var (net, gross) = financialService.Calculate(request.Net, request.Gross);

            cost.CostEstimateId = request.CostEstimateId;
            cost.CostEstimateItemId = request.CostEstimateItemId;
            cost.Name = request.Name;
            cost.Description = request.Description;
            cost.Net = net;
            cost.Gross = gross;
            cost.Contractor = request.Contractor;
            cost.Date = request.Date;
            cost.UpdatedAt = DateTime.UtcNow;

            await trackedCostRepository.Update(cost);

            logger.LogInformation(
                "Updated TrackedCost {CostId} for tracker {TrackerId} by user {UserId}",
                cost.Id, cost.TrackerId, currentUser.Id);

            var attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, request.ExistingAttachmentIds, request.TenantId, request.ProjectId, cancellationToken);

            return BuildCostWeb(cost, attachments);
        }
    }
}
