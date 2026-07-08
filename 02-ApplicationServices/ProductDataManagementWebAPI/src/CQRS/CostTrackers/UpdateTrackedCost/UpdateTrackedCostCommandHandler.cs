using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using CQRS.Projects.Shared;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
        private readonly IRepository<ProjectCost> projectCostRepository;
        private readonly IReadRepository<ProjectCostCategory> categoryRepo;
        private readonly ICostTrackerFinancialService financialService;
        private readonly ILogger<UpdateTrackedCostCommandHandler> logger;

        public UpdateTrackedCostCommandHandler(
            IReadRepository<TrackedCost> trackedCostRepository,
            IRepository<ProjectCost> projectCostRepository,
            IReadRepository<ProjectCostCategory> categoryRepo,
            IReadRepository<CostEstimateItem> costEstimateItemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            IContractorService contractorService,
            ICurrentUser currentUser,
            ILogger<UpdateTrackedCostCommandHandler> logger)
            : base(currentUser, trackedCostRepository, costEstimateItemRepository, stageWorkRepository, attachmentService, contractorService)
        {
            this.trackedCostRepository = trackedCostRepository;
            this.projectCostRepository = projectCostRepository;
            this.categoryRepo = categoryRepo;
            this.financialService = financialService;
            this.logger = logger;
        }

        public async Task<TrackedCostWeb> Handle(
            UpdateTrackedCostCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

            BaseCost cost = await ResolveEditableCostAsync(
                request.CostId, request.TenantId, request.ProjectId, cancellationToken);

            (decimal? net, decimal? gross) = financialService.Calculate(request.Net, request.Gross);

            await ValidateTrackedCostLinksAsync(
                request.CostEstimateItemId, request.WorkScheduleStageWorkId,
                request.ProjectId, request.TenantId, cancellationToken);

            await ProjectCostCategoryValidation.ValidateCategoryBelongsToProjectAsync(
                request.CategoryId, request.ProjectId, categoryRepo, cancellationToken);

            cost.Name = request.Name;
            cost.Number = request.Number;
            cost.Description = request.Description;
            cost.Net = net;
            cost.Gross = gross;
            cost.ContractorId = request.ContractorId;
            cost.CategoryId = request.CategoryId;
            cost.Date = request.Date;
            cost.CostEstimateItemId = request.CostEstimateItemId;
            cost.WorkScheduleStageWorkId = request.WorkScheduleStageWorkId;
            cost.UpdatedAt = DateTime.UtcNow;

            await PersistCostAsync(cost);

            logger.LogInformation(
                "Updated cost {CostId} for project {ProjectId} by user {UserId}",
                cost.Id, cost.ProjectId, currentUser.Id);

            IReadOnlyList<Guid>? effectiveExistingIds = request.ClearAllAttachments
                ? new List<Guid>()
                : request.ExistingAttachmentIds;

            List<BaseCostAttachment> attachments = await attachmentService.SyncAttachmentsAsync(
                cost, request.NewFiles, effectiveExistingIds, request.TenantId, request.ProjectId, cancellationToken);

            await LoadContractorNamesAsync(new List<BaseCost> { cost }, request.TenantId, cancellationToken);
            await LoadCategoryInfoAsync(new List<BaseCost> { cost }, request.ProjectId, categoryRepo, cancellationToken);

            return MapCostToWeb(cost, attachments);
        }

        private async Task<BaseCost> ResolveEditableCostAsync(
            Guid costId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            TrackedCost? trackedCost = await trackedCostRepository.GetFirstBySearch(
                tc => tc.Id == costId && tc.TenantId == tenantId && tc.ProjectId == projectId);

            if (trackedCost is not null)
            {
                return trackedCost;
            }

            ProjectCost? projectCost = await projectCostRepository.GetFirstBySearch(
                pc => pc.Id == costId && pc.TenantId == tenantId && pc.ProjectId == projectId
                      && pc.ApprovalStatus == CostApprovalStatus.Approved);

            if (projectCost is not null)
            {
                return projectCost;
            }

            throw new NotFoundApiException(nameof(TrackedCost), costId.ToString());
        }

        private async Task PersistCostAsync(BaseCost cost)
        {
            if (cost is TrackedCost trackedCost)
            {
                await trackedCostRepository.Update(trackedCost);
                return;
            }

            if (cost is ProjectCost projectCost)
            {
                await projectCostRepository.Update(projectCost);
            }
        }
    }
}
