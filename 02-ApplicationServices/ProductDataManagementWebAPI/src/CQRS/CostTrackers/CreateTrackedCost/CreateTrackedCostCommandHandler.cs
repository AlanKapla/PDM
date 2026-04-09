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

namespace CQRS.CostTrackers.CreateTrackedCost
{
    public sealed class CreateTrackedCostCommandHandler
        : TrackedCostMutationHandlerBase, IRequestHandler<CreateTrackedCostCommand, TrackedCostWeb>
    {
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly ICostTrackerFinancialService financialService;
        private readonly ILogger<CreateTrackedCostCommandHandler> logger;

        public CreateTrackedCostCommandHandler(
            IReadRepository<CostTracker> trackerRepository,
            IReadRepository<Project> projectRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateItem> itemRepository,
            IReadRepository<TrackedCost> trackedCostRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser,
            ILogger<CreateTrackedCostCommandHandler> logger)
            : base(trackerRepository, projectRepository, costEstimateRepository, itemRepository, attachmentService, trackedCostRepository, currentUser)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.financialService = financialService;
            this.logger = logger;
        }

        public async Task<TrackedCostWeb> Handle(
            CreateTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            CostTracker tracker = await LoadTracker(request.TenantId, request.ProjectId, cancellationToken);

            await ValidateCostEstimateAndItemAsync(request.CostEstimateId, request.ProjectId, request.CostEstimateItemId, cancellationToken);

            (decimal? net, decimal? gross) = financialService.Calculate(request.Net, request.Gross);

            TrackedCost cost = new TrackedCost
            {
                TrackerId = tracker.Id,
                CostEstimateId = request.CostEstimateId,
                CostEstimateItemId = request.CostEstimateItemId,
                Name = request.Name,
                Description = request.Description,
                Net = net,
                Gross = gross,
                Contractor = request.Contractor,
                Date = request.Date,
                CreatedAt = DateTime.UtcNow
            };

            await trackedCostRepository.Insert(cost);
            await trackedCostRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created TrackedCost {CostId} for tracker {TrackerId} by user {UserId}",
                cost.Id, tracker.Id, currentUser.Id);

            List<TrackedCostAttachment> attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, [], request.TenantId, request.ProjectId, cancellationToken);

            return BuildCostWeb(cost, attachments);
        }
    }
}

