using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostTrackers;
using Entities.Models.WorkItemLinks;
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
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser,
            ILogger<CreateTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, workItemLinkRepository, attachmentService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.financialService = financialService;
            this.logger = logger;
        }

        public async Task<TrackedCostWeb> Handle(
            CreateTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateWorkItemLinkAsync(request.WorkItemLinkId, request.ProjectId, cancellationToken);

            (decimal? net, decimal? gross) = financialService.Calculate(request.Net, request.Gross);

            TrackedCost cost = new TrackedCost
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                WorkItemLinkId = request.WorkItemLinkId,
                CostEstimateItemId = request.CostEstimateItemId,
                WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                Name = request.Name,
                Number = request.Number,
                Description = request.Description,
                Net = net,
                Gross = gross,
                Contractor = request.Contractor,
                Date = request.Date,
                CreatedAt = DateTime.UtcNow
            };

            cost.ValidateLinkExclusivity();

            await trackedCostRepository.Insert(cost);
            await trackedCostRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created TrackedCost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            List<TrackedCostAttachment> attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, [], request.TenantId, request.ProjectId, cancellationToken);

            return BuildCostWeb(cost, attachments);
        }
    }
}

