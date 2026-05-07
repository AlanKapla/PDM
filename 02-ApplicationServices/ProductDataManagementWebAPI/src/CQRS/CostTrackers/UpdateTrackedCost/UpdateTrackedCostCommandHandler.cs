using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
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
            IReadRepository<CostEstimateItem> costEstimateItemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser,
            ILogger<UpdateTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, costEstimateItemRepository, stageWorkRepository, attachmentService)
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

            (decimal? net, decimal? gross) = financialService.Calculate(request.Net, request.Gross);

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

            List<BaseCostAttachment> attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, effectiveExistingIds, request.TenantId, request.ProjectId, cancellationToken);

            return BuildCostWeb(cost, attachments);
        }
    }
}
