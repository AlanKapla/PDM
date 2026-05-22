using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public sealed class CopyCostEstimateCommandHandler : IRequestHandler<CopyCostEstimateCommand, List<Guid>>
    {
        private readonly IRepository<CostEstimate> costEstimateRepo;
        private readonly IRepository<CostEstimateGroup> groupRepo;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepo;
        private readonly IRepository<CostEstimateItem> itemRepo;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepo;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public CopyCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<CostEstimateGroup> groupRepo,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepo,
            IRepository<CostEstimateItem> itemRepo,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepo,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepo = costEstimateRepo;
            this.groupRepo = groupRepo;
            this.groupFieldValueRepo = groupFieldValueRepo;
            this.itemRepo = itemRepo;
            this.itemFieldValueRepo = itemFieldValueRepo;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<List<Guid>> Handle(CopyCostEstimateCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid costEstimateId = request.CostEstimateId;

            // 1. Verify source cost estimate exists and belongs to the correct project/tenant
            CostEstimate sourceCostEstimate = await costEstimateRepo.GetFirstBySearch(
                ce => ce.Id == costEstimateId
                    && ce.TenantId == tenantId
                    && ce.ProjectId == request.ProjectId
                    && !ce.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimate), costEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, tenantId, request.ProjectId, costEstimateId, cancellationToken);

            if (accessLevel != CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("Only the owner or an admin can copy this cost estimate.");
            }

            // 2. Load full hierarchy via separate batch queries (avoids 3-level Include chain timeouts)
            IEnumerable<CostEstimateGroup> sourceGroupsQuery = await groupRepo.GetBySearch(
                g => g.CostEstimateId == costEstimateId && !g.IsDeleted);
            List<CostEstimateGroup> sourceGroups = sourceGroupsQuery.ToList();

            IEnumerable<CostEstimateItem> sourceItemsQuery = await itemRepo.GetBySearch(
                i => i.CostEstimateId == costEstimateId && !i.IsDeleted);
            List<CostEstimateItem> sourceItems = sourceItemsQuery.ToList();

            IEnumerable<CostEstimateGroupFieldValue> sourceGroupFieldValuesQuery = await groupFieldValueRepo.GetBySearch(
                fv => fv.Group.CostEstimateId == costEstimateId);
            List<CostEstimateGroupFieldValue> sourceGroupFieldValues = sourceGroupFieldValuesQuery.ToList();

            IEnumerable<CostEstimateItemFieldValue> sourceItemFieldValuesQuery = await itemFieldValueRepo.GetBySearch(
                fv => fv.Item.CostEstimateId == costEstimateId);
            List<CostEstimateItemFieldValue> sourceItemFieldValues = sourceItemFieldValuesQuery.ToList();

            // 3. Build lookup dictionaries to replace navigation property access
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId = sourceGroupFieldValues
                .GroupBy(fv => fv.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId = sourceItemFieldValues
                .GroupBy(fv => fv.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = sourceItems
                .Where(i => i.ParentItemId == null)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> optionsByParentItemId = sourceItems
                .Where(i => i.ParentItemId != null)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<Guid> createdCostEstimateIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            // 4. Create copy for each target project
            foreach (Guid targetProjectId in request.TargetProjectIds)
            {
                // Create new cost estimate
                CostEstimate copiedCostEstimate = new CostEstimate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProjectId = targetProjectId,
                    TemplateId = sourceCostEstimate.TemplateId,
                    OwnerId = currentUser.Id,
                    Name = $"{sourceCostEstimate.Name} (kopia)",
                    Description = sourceCostEstimate.Description,
                    Status = CostEstimateStatus.Draft,
                    TotalNet = sourceCostEstimate.TotalNet,
                    TotalGross = sourceCostEstimate.TotalGross,
                    TotalVat = sourceCostEstimate.TotalVat,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastCalculatedAt = sourceCostEstimate.LastCalculatedAt,
                    IsDeleted = false
                };

                await costEstimateRepo.Insert(copiedCostEstimate);
                await costEstimateRepo.SaveChangesAsync(cancellationToken);

                // Deep copy groups and work scope items
                Dictionary<Guid, Guid> groupIdMapping = new Dictionary<Guid, Guid>(); // old ID => new ID
                List<CostEstimateGroup> allCopiedGroups = new List<CostEstimateGroup>();
                List<CostEstimateGroupFieldValue> allCopiedGroupFieldValues = new List<CostEstimateGroupFieldValue>();
                List<CostEstimateItem> allCopiedItems = new List<CostEstimateItem>();
                List<CostEstimateItemFieldValue> allCopiedItemFieldValues = new List<CostEstimateItemFieldValue>();

                // Copy all groups (maintain hierarchy)
                foreach (CostEstimateGroup sourceGroup in sourceGroups.OrderBy(g => g.Level))
                {
                    Guid newGroupId = Guid.NewGuid();
                    groupIdMapping[sourceGroup.Id] = newGroupId;

                    allCopiedGroups.Add(new CostEstimateGroup
                    {
                        Id = newGroupId,
                        CostEstimateId = copiedCostEstimate.Id,
                        ParentGroupId = sourceGroup.ParentGroupId.HasValue 
                            ? groupIdMapping.GetValueOrDefault(sourceGroup.ParentGroupId.Value) 
                            : null,
                        Level = sourceGroup.Level,
                        Order = sourceGroup.Order,
                        TotalNet = sourceGroup.TotalNet,
                        TotalGross = sourceGroup.TotalGross,
                        TotalVat = sourceGroup.TotalVat,
                        LastCalculatedAt = sourceGroup.LastCalculatedAt,
                        CreatedAt = now,
                        IsDeleted = false
                    });

                    // Copy group field values
                    if (groupFieldValuesByGroupId.TryGetValue(sourceGroup.Id, out List<CostEstimateGroupFieldValue>? sourceGroupFvs))
                    {
                        foreach (CostEstimateGroupFieldValue sourceFieldValue in sourceGroupFvs)
                        {
                            allCopiedGroupFieldValues.Add(new CostEstimateGroupFieldValue
                            {
                                Id = Guid.NewGuid(),
                                GroupId = newGroupId,
                                FieldDefinitionId = sourceFieldValue.FieldDefinitionId,
                                StringValue = sourceFieldValue.StringValue,
                                DecimalValue = sourceFieldValue.DecimalValue,
                                BoolValue = sourceFieldValue.BoolValue,
                                DateTimeValue = sourceFieldValue.DateTimeValue,
                                CreatedAt = now
                            });
                        }
                    }

                    // Copy work scope items for this group (tylko główne pozycje - ParentItemId == null)
                    List<CostEstimateItem> mainItems = mainItemsByGroupId.TryGetValue(sourceGroup.Id, out List<CostEstimateItem>? items)
                        ? items
                        : new List<CostEstimateItem>();
                    Dictionary<Guid, Guid> itemIdMapping = new Dictionary<Guid, Guid>(); // old item ID => new item ID

                    foreach (CostEstimateItem sourceItem in mainItems)
                    {
                        CollectCopiedItems(
                            copiedCostEstimate.Id,
                            newGroupId,
                            null,
                            sourceItem,
                            itemIdMapping,
                            now,
                            allCopiedItems,
                            allCopiedItemFieldValues,
                            optionsByParentItemId,
                            itemFieldValuesByItemId);
                    }
                }

                // Batch insert all collected entities
                await groupRepo.InsertRange(allCopiedGroups);
                await groupFieldValueRepo.InsertRange(allCopiedGroupFieldValues);
                await itemRepo.InsertRange(allCopiedItems);
                await itemFieldValueRepo.InsertRange(allCopiedItemFieldValues);

                await groupRepo.SaveChangesAsync(cancellationToken);

                createdCostEstimateIds.Add(copiedCostEstimate.Id);
            }

            return createdCostEstimateIds;
        }

        private static void CollectCopiedItems(
            Guid costEstimateId,
            Guid groupId,
            Guid? parentItemId,
            CostEstimateItem sourceItem,
            Dictionary<Guid, Guid> itemIdMapping,
            DateTime now,
            List<CostEstimateItem> allItems,
            List<CostEstimateItemFieldValue> allFieldValues,
            Dictionary<Guid, List<CostEstimateItem>> optionsByParentItemId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId)
        {
            Guid newItemId = Guid.NewGuid();
            itemIdMapping[sourceItem.Id] = newItemId;

            allItems.Add(new CostEstimateItem
            {
                Id = newItemId,
                CostEstimateId = costEstimateId,
                GroupId = groupId,
                ParentItemId = parentItemId,
                Order = sourceItem.Order,
                CreatedAt = now,
                IsDeleted = false
            });

            // Copy field values
            if (itemFieldValuesByItemId.TryGetValue(sourceItem.Id, out List<CostEstimateItemFieldValue>? sourceFvs))
            {
                foreach (CostEstimateItemFieldValue sourceFieldValue in sourceFvs)
                {
                    allFieldValues.Add(new CostEstimateItemFieldValue
                    {
                        Id = Guid.NewGuid(),
                        ItemId = newItemId,
                        FieldDefinitionId = sourceFieldValue.FieldDefinitionId,
                        StringValue = sourceFieldValue.StringValue,
                        DecimalValue = sourceFieldValue.DecimalValue,
                        BoolValue = sourceFieldValue.BoolValue,
                        DateTimeValue = sourceFieldValue.DateTimeValue,
                        CreatedAt = now
                    });
                }
            }

            // Recursively collect options
            if (optionsByParentItemId.TryGetValue(sourceItem.Id, out List<CostEstimateItem>? sourceOptions))
            {
                foreach (CostEstimateItem sourceOption in sourceOptions)
                {
                    CollectCopiedItems(
                        costEstimateId,
                        groupId,
                        newItemId,
                        sourceOption,
                        itemIdMapping,
                        now,
                        allItems,
                        allFieldValues,
                        optionsByParentItemId,
                        itemFieldValuesByItemId);
                }
            }
        }
    }
}
