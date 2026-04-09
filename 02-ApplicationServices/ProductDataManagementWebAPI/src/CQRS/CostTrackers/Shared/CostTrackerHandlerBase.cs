using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.Shared
{
    public abstract class CostTrackerHandlerBase
    {
        protected readonly IReadRepository<CostTracker> trackerRepository;
        private readonly IReadRepository<TrackedCost> trackedCostRepository;
        private readonly ICostTrackerAttachmentService attachmentService;
        protected readonly ICurrentUser currentUser;
        protected readonly ICostTrackerFinancialService? financialService;

        protected CostTrackerHandlerBase(
            IReadRepository<CostTracker> trackerRepository,
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService)
        {
            this.trackerRepository = trackerRepository;
            this.currentUser = currentUser;
            this.trackedCostRepository = trackedCostRepository;
            this.attachmentService = attachmentService;
        }

        protected CostTrackerHandlerBase(
            IReadRepository<CostTracker> trackerRepository,
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository,
            ICostTrackerAttachmentService attachmentService,
            ICostTrackerFinancialService financialService)
            : this(trackerRepository, currentUser, trackedCostRepository, attachmentService)
        {
            this.financialService = financialService;
        }

        protected CostTrackerHandlerBase(
            IReadRepository<CostTracker> trackerRepository,
            ICurrentUser currentUser,
            IReadRepository<TrackedCost> trackedCostRepository)
        {
            this.trackerRepository = trackerRepository;
            this.currentUser = currentUser;
            this.trackedCostRepository = trackedCostRepository;
        }

        protected async Task ValidateAccessAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            if (!await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                throw new ForbiddenApiException("User does not have access to this resource.");
            }
        }

        protected async Task<CostTracker> LoadTracker(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            if (!await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                throw new ForbiddenApiException("User does not have access to this resource.");
            }

            CostTracker tracker = await trackerRepository.GetFirstBySearch(t => t.ProjectId == projectId && t.TenantId == tenantId)
                ?? throw new NotFoundApiException(nameof(CostTracker), projectId.ToString());

            return tracker;
        }

        protected async Task<TrackedCost> GetAndValidateTrackedCostAsync(
            Guid costId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            if (!await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                throw new ForbiddenApiException("User does not have access to this resource.");
            }

            return await trackedCostRepository.GetFirstBySearch(
                tc => tc.Id == costId && tc.Tracker.TenantId == tenantId && tc.Tracker.ProjectId == projectId)
                ?? throw new NotFoundApiException(nameof(TrackedCost), costId.ToString());
        }

        protected TrackedCostWeb MapTrackedCostToWeb(
            TrackedCost cost,
            IEnumerable<TrackedCostAttachment> attachments)
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
                TrackerId = cost.TrackerId,
                CostEstimateId = cost.CostEstimateId,
                CostEstimateItemId = cost.CostEstimateItemId,
                IsAdditional = !cost.CostEstimateId.HasValue,
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

        protected CostEstimateSummaryWeb BuildEstimateSummary(
            CostEstimate costEstimate,
            Dictionary<Guid, CostEstimateItem> itemsDict,
            ILookup<Guid, TrackedCost> costsByItemId,
            List<TrackedCost> additionalCostsList,
            List<TrackerGroupWeb> groups,
            List<TrackedCostWeb> additionalCostWebs)
        {
            decimal? additionalNet = additionalCostsList.Any(c => c.Net.HasValue)
                ? additionalCostsList.Sum(c => c.Net ?? 0)
                : null;

            decimal? additionalGross = additionalCostsList.Any(c => c.Gross.HasValue)
                ? additionalCostsList.Sum(c => c.Gross ?? 0)
                : null;

            List<CostEstimateItem> allMainItems = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .ToList();

            CostEstimateSummaryWeb summary = financialService!.ComputeEstimateSummary(
                costEstimate: costEstimate,
                budgetItems: allMainItems,
                costsByItemId: costsByItemId,
                additionalCostsNet: additionalNet,
                additionalCostsGross: additionalGross,
                additionalCostsCount: additionalCostsList.Count);

            return summary with
            {
                Groups = groups,
                AdditionalCosts = summary.AdditionalCosts with
                {
                    Costs = additionalCostWebs
                }
            };
        }

        protected List<TrackerGroupWeb> BuildTrackerGroups(
            Dictionary<Guid, CostEstimateGroup> groupsDict,
            Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict,
            Dictionary<Guid, CostEstimateItem> itemsDict,
            Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId)
        {
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId = groupFieldValuesDict.Values
                .GroupBy(fv => fv.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId = itemFieldValuesDict.Values
                .GroupBy(fv => fv.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId = groupsDict.Values
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            List<CostEstimateGroup> rootGroups = groupsDict.Values
                .Where(g => g.ParentGroupId == null)
                .OrderBy(g => g.Order)
                .ToList();

            return BuildTrackerGroupHierarchy(
                rootGroups,
                childGroupsByParentId,
                mainItemsByGroupId,
                groupFieldValuesByGroupId,
                itemFieldValuesByItemId,
                costsByItemId,
                attachmentsByCostId);
        }

        private List<TrackerGroupWeb> BuildTrackerGroupHierarchy(
            List<CostEstimateGroup> currentLevelGroups,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group => BuildTrackerGroupWeb(
                    group, childGroupsByParentId, mainItemsByGroupId,
                    groupFieldValuesByGroupId, itemFieldValuesByItemId, costsByItemId, attachmentsByCostId))
                .ToList();
        }

        private TrackerGroupWeb BuildTrackerGroupWeb(
            CostEstimateGroup group,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId)
        {
            List<CostEstimateGroup> children = childGroupsByParentId.TryGetValue(group.Id, out List<CostEstimateGroup>? childList)
                ? childList : [];

            List<TrackerGroupWeb> childGroups = BuildTrackerGroupHierarchy(
                children, childGroupsByParentId, mainItemsByGroupId,
                groupFieldValuesByGroupId, itemFieldValuesByItemId, costsByItemId, attachmentsByCostId);

            List<CostEstimateItem> groupItems = mainItemsByGroupId.TryGetValue(group.Id, out List<CostEstimateItem>? items)
                ? items : [];

            List<TrackerItemWeb> trackerItems = groupItems
                .Select(item => BuildTrackerItemWeb(item, itemFieldValuesByItemId, costsByItemId, attachmentsByCostId))
                .ToList();

            List<TrackedCost> directCosts = groupItems.SelectMany(i => costsByItemId[i.Id]).ToList();
            decimal? directCostsNet = directCosts.Any(c => c.Net.HasValue) ? directCosts.Sum(c => c.Net ?? 0) : null;
            decimal? directCostsGross = directCosts.Any(c => c.Gross.HasValue) ? directCosts.Sum(c => c.Gross ?? 0) : null;
            decimal? directBudgetNet = groupItems.Any(i => i.NetValue.HasValue) ? groupItems.Sum(i => i.NetValue ?? 0) : null;
            decimal? directBudgetGross = groupItems.Any(i => i.GrossValue.HasValue) ? groupItems.Sum(i => i.GrossValue ?? 0) : null;

            decimal? groupCostsNet = CombineNullable(directCostsNet, AggregateNullable(childGroups.Select(g => g.CostsNet)));
            decimal? groupCostsGross = CombineNullable(directCostsGross, AggregateNullable(childGroups.Select(g => g.CostsGross)));
            decimal? groupBudgetNet = CombineNullable(directBudgetNet, AggregateNullable(childGroups.Select(g => g.BudgetNet)));
            decimal? groupBudgetGross = CombineNullable(directBudgetGross, AggregateNullable(childGroups.Select(g => g.BudgetGross)));

            decimal? groupDeviationNet = groupBudgetNet.HasValue && groupCostsNet.HasValue
                ? Math.Round(groupCostsNet.Value - groupBudgetNet.Value, 2)
                : null;
            decimal? groupDeviationPercent = groupBudgetNet.HasValue && groupBudgetNet.Value != 0 && groupCostsNet.HasValue
                ? Math.Round((groupCostsNet.Value - groupBudgetNet.Value) / groupBudgetNet.Value * 100, 2)
                : null;

            int directCostCount = groupItems.Sum(i => costsByItemId[i.Id].Count());
            int groupCostCount = directCostCount + childGroups.Sum(g => g.CostCount);
            int groupStatus = (int)financialService!.ComputeItemStatus(groupBudgetNet, groupCostsNet, groupCostCount);

            int directItemsWithCosts = groupItems.Count(i => costsByItemId[i.Id].Any());
            int childItemsWithCosts = childGroups.Sum(g => g.ItemsWithCostsCount);
            int childTotalItems = childGroups.Sum(g => g.TotalItemsCount);
            int totalItemsInGroup = groupItems.Count + childTotalItems;
            int itemsWithCostsInGroup = directItemsWithCosts + childItemsWithCosts;
            decimal? groupCoveredPercent = totalItemsInGroup > 0
                ? Math.Round((decimal)itemsWithCostsInGroup / totalItemsInGroup * 100, 2)
                : null;

            return new TrackerGroupWeb
            {
                GroupId = group.Id,
                GroupName = ResolveGroupName(group.Id, groupFieldValuesByGroupId),
                Order = group.Order,
                BudgetNet = groupBudgetNet,
                BudgetGross = groupBudgetGross,
                CostsNet = groupCostsNet,
                CostsGross = groupCostsGross,
                DeviationNet = groupDeviationNet,
                DeviationPercent = groupDeviationPercent,
                IsBudgetExceeded = groupDeviationNet.HasValue && groupDeviationNet.Value > 0,
                Status = groupStatus,
                CostCount = groupCostCount,
                CoveredPercent = groupCoveredPercent,
                TotalItemsCount = totalItemsInGroup,
                ItemsWithCostsCount = itemsWithCostsInGroup,
                Items = trackerItems,
                ChildGroups = childGroups
            };
        }

        private TrackerItemWeb BuildTrackerItemWeb(
            CostEstimateItem item,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId,
            ILookup<Guid, TrackedCost> costsByItemId,
            ILookup<Guid, TrackedCostAttachment> attachmentsByCostId)
        {
            List<TrackedCost> itemCostsList = costsByItemId[item.Id].ToList();
            decimal? itemCostsNet = itemCostsList.Any(c => c.Net.HasValue) ? itemCostsList.Sum(c => c.Net ?? 0) : null;
            decimal? itemCostsGross = itemCostsList.Any(c => c.Gross.HasValue) ? itemCostsList.Sum(c => c.Gross ?? 0) : null;
            decimal? itemDeviationNet = item.NetValue.HasValue && itemCostsNet.HasValue
                ? Math.Round(itemCostsNet.Value - item.NetValue.Value, 2)
                : null;
            decimal? itemDeviationPercent = item.NetValue.HasValue && item.NetValue.Value != 0 && itemCostsNet.HasValue
                ? Math.Round((itemCostsNet.Value - item.NetValue.Value) / item.NetValue.Value * 100, 2)
                : null;

            List<TrackedCostWeb> costWebs = itemCostsList
                .Select(c => MapTrackedCostToWeb(c, attachmentsByCostId[c.Id]))
                .ToList();

            return new TrackerItemWeb
            {
                CostEstimateItemId = item.Id,
                Name = ResolveItemName(item.Id, itemFieldValuesByItemId),
                BudgetNet = item.NetValue,
                BudgetGross = item.GrossValue,
                CostsNet = itemCostsNet,
                CostsGross = itemCostsGross,
                DeviationNet = itemDeviationNet,
                DeviationPercent = itemDeviationPercent,
                IsBudgetExceeded = itemDeviationNet.HasValue && itemDeviationNet.Value > 0,
                Status = (int)financialService!.ComputeItemStatus(item.NetValue, itemCostsNet, itemCostsList.Count),
                CostCount = itemCostsList.Count,
                CoveredPercent = itemCostsList.Count > 0 ? 100.0m : 0.0m,
                Costs = costWebs
            };
        }

        private static string ResolveGroupName(
            Guid groupId,
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId)
        {
            if (!groupFieldValuesByGroupId.TryGetValue(groupId, out List<CostEstimateGroupFieldValue>? fieldValues))
            {
                return string.Empty;
            }

            return fieldValues.FirstOrDefault(fv => fv.FieldDefinition.FieldType == FieldType.GroupName)?.StringValue
                ?? string.Empty;
        }

        private static string ResolveItemName(
            Guid itemId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId)
        {
            if (!itemFieldValuesByItemId.TryGetValue(itemId, out List<CostEstimateItemFieldValue>? fieldValues))
            {
                return string.Empty;
            }

            return fieldValues.FirstOrDefault(fv => fv.FieldDefinition.FieldType == FieldType.ItemSystemName)?.StringValue
                ?? string.Empty;
        }

        private static decimal? CombineNullable(decimal? a, decimal? b)
        {
            if (!a.HasValue && !b.HasValue)
            {
                return null;
            }

            return (a ?? 0) + (b ?? 0);
        }

        private static decimal? AggregateNullable(IEnumerable<decimal?> values)
        {
            List<decimal?> list = values.ToList();

            if (!list.Any(v => v.HasValue))
            {
                return null;
            }

            return list.Sum(v => v ?? 0);
        }
    }
}
