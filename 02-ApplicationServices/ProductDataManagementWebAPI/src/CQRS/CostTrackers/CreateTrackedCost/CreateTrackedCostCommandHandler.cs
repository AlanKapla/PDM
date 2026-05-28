using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using Entities.Models.Chats;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
            IReadRepository<CostEstimateItem> costEstimateItemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<CreateTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, costEstimateItemRepository, stageWorkRepository, attachmentService, contractorService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.financialService = financialService;
            this.logger = logger;
        }

        public async Task<TrackedCostWeb> Handle(
            CreateTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateTrackedCostLinksAsync(
                request.CostEstimateItemId, request.WorkScheduleStageWorkId,
                request.ProjectId, request.TenantId, cancellationToken);

            (decimal? net, decimal? gross) = financialService.Calculate(request.Net, request.Gross);

            TrackedCost cost = new TrackedCost
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                CostEstimateItemId = request.CostEstimateItemId,
                WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                Name = request.Name,
                Number = request.Number,
                Description = request.Description,
                Net = net,
                Gross = gross,
                ContractorId = request.ContractorId,
                Date = request.Date,
                CreatedAt = DateTime.UtcNow
            };

            await trackedCostRepository.Insert(cost);
            await trackedCostRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created TrackedCost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            List<BaseCostAttachment> attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, [], request.TenantId, request.ProjectId, cancellationToken);

            await LoadContractorNamesAsync(new List<BaseCost> { cost }, request.TenantId, cancellationToken);

            return MapTrackedCostToWeb(cost, attachments);
        }
    }
}

