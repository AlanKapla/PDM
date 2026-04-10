using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.GetCostTrackerByProject
{
    public sealed class GetCostTrackerByProjectQueryHandler
        : CostTrackerHandlerBase, IRequestHandler<GetCostTrackerByProjectQuery, CostTrackerDetailsWeb>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly IReadRepository<TrackedCostAttachment> attachmentRepository;
        private readonly ICostEstimateCacheService ceCacheService;

        public GetCostTrackerByProjectQueryHandler(
            IReadRepository<CostTracker> trackerRepository,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<TrackedCostAttachment> attachmentRepository,
            ICostEstimateCacheService ceCacheService,
            ICostTrackerFinancialService financialService,
            ICostTrackerAttachmentService attachmentService,
            ICurrentUser currentUser)
            : base(trackerRepository, currentUser, trackedCostRepository, attachmentService, financialService)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.attachmentRepository = attachmentRepository;
            this.ceCacheService = ceCacheService;
        }

        public async Task<CostTrackerDetailsWeb> Handle(
            GetCostTrackerByProjectQuery request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

            List<TrackedCost> allTrackedCosts = await LoadTrackedCostsAsync(request.TenantId, request.ProjectId);

            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId = await LoadAttachmentLookupAsync(allTrackedCosts);

            ProjectAdditionalCostsWeb projectAdditionalCosts = BuildProjectAdditionalCosts(allTrackedCosts, attachmentsByCostId);

            List<TrackedCost> estimateScopedCosts = allTrackedCosts
                .Where(tc => tc.CostEstimateId.HasValue)
                .ToList();

            List<CostEstimate> allEstimates = (await costEstimateRepository.GetBySearch(
                ce => ce.ProjectId == request.ProjectId && ce.TenantId == request.TenantId && !ce.IsDeleted)).ToList();

            List<CostEstimateSummaryWeb> estimateSummaries = await BuildEstimateSummariesAsync(
                allEstimates, estimateScopedCosts, attachmentsByCostId, request.TenantId, request.ProjectId, cancellationToken);

            CostTracker tracker = await LoadTrackerEntityAsync(request.TenantId, request.ProjectId, cancellationToken);

            CostTrackerSummaryWeb projectSummary = financialService!.ComputeProjectSummary(estimateSummaries, projectAdditionalCosts, tracker.BudgetNet, tracker.BudgetGross);

            CostTrackerBudgetSummary budgetSummary = financialService!.ComputeBudgetSummary(projectAdditionalCosts, tracker.BudgetNet, tracker.BudgetGross);

            return new CostTrackerDetailsWeb
            {
                Id = tracker.Id,
                ProjectId = request.ProjectId,
                Summary = projectSummary,
                BudgetSummary = budgetSummary,
                CostEstimateSummaries = estimateSummaries,
                ProjectAdditionalCosts = projectAdditionalCosts
            };
        }

        private async Task<CostTracker> LoadTrackerEntityAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            return await trackerRepository.GetFirstBySearch(
                t => t.TenantId == tenantId && t.ProjectId == projectId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostTracker), projectId.ToString());
        }

        private async Task<List<TrackedCost>> LoadTrackedCostsAsync(Guid tenantId, Guid projectId)
        {
            return (await trackedCostRepository.GetBySearch(
                tc => tc.Tracker.TenantId == tenantId && tc.Tracker.ProjectId == projectId && !tc.IsDeleted))
                .ToList();
        }

        private async Task<ILookup<Guid, TrackedCostAttachment>> LoadAttachmentLookupAsync(
            List<TrackedCost> trackedCosts)
        {
            HashSet<Guid> costIds = trackedCosts.Select(tc => tc.Id).ToHashSet();

            List<TrackedCostAttachment> allAttachments = costIds.Count > 0
                ? (await attachmentRepository.GetBySearch(a => costIds.Contains(a.TrackedCostId))).ToList()
                : new List<TrackedCostAttachment>();

            return allAttachments.ToLookup(a => a.TrackedCostId);
        }

        private ProjectAdditionalCostsWeb BuildProjectAdditionalCosts(
            List<TrackedCost> allTrackedCosts,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId)
        {
            List<TrackedCost> projectAdditionalCostsList = allTrackedCosts
                .Where(tc => !tc.CostEstimateId.HasValue)
                .ToList();

            decimal? totalNet = projectAdditionalCostsList.Any(c => c.Net.HasValue)
                ? projectAdditionalCostsList.Sum(c => c.Net ?? 0)
                : null;

            decimal? totalGross = projectAdditionalCostsList.Any(c => c.Gross.HasValue)
                ? projectAdditionalCostsList.Sum(c => c.Gross ?? 0)
                : null;

            return new ProjectAdditionalCostsWeb
            {
                TotalNet = totalNet,
                TotalGross = totalGross,
                CostsCount = projectAdditionalCostsList.Count,
                Costs = projectAdditionalCostsList.Select(c => MapTrackedCostToWeb(c, attachmentsByCostId[c.Id])).ToList()
            };
        }

        private async Task<List<CostEstimateSummaryWeb>> BuildEstimateSummariesAsync(
            List<CostEstimate> allEstimates,
            List<TrackedCost> estimateScopedCosts,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            List<CostEstimateSummaryWeb> summaries = new List<CostEstimateSummaryWeb>();

            foreach (CostEstimate costEstimate in allEstimates)
            {
                Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);
                Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict = await ceCacheService.GetGroupFieldValuesDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);
                Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);
                Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict = await ceCacheService.GetItemFieldValuesDictionaryAsync(
                    costEstimate.Id, tenantId, projectId, cancellationToken);

                List<TrackedCost> estimateCosts = estimateScopedCosts
                    .Where(tc => tc.CostEstimateId == costEstimate.Id)
                    .ToList();

                ILookup<Guid, TrackedCost> costsByItemId = estimateCosts
                    .Where(tc => tc.CostEstimateItemId.HasValue)
                    .ToLookup(tc => tc.CostEstimateItemId!.Value);

                List<TrackedCost> additionalCostsList = estimateCosts
                    .Where(tc => !tc.CostEstimateItemId.HasValue)
                    .ToList();

                List<TrackerGroupWeb> groups = BuildTrackerGroups(
                    groupsDict, groupFieldValuesDict, itemsDict, itemFieldValuesDict, costsByItemId, attachmentsByCostId);

                List<TrackedCostWeb> additionalCostWebs = additionalCostsList
                    .Select(tc => MapTrackedCostToWeb(tc, attachmentsByCostId[tc.Id]))
                    .ToList();

                summaries.Add(BuildEstimateSummary(costEstimate, itemsDict, costsByItemId, additionalCostsList, groups, additionalCostWebs));
            }

            return summaries;
        }

    }
}
