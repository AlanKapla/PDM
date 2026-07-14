using Business.Implementation.Helpers;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public sealed class CopyCostEstimateCommandHandler : IRequestHandler<CopyCostEstimateCommand, List<Guid>>
    {
        private readonly IRepository<CostEstimate> costEstimateRepo;
        private readonly IRepository<CostEstimateGroup> groupRepo;
        private readonly IRepository<CostEstimateItem> itemRepo;
        private readonly IRepository<CostEstimateAdditionalFieldValue> additionalFieldValueRepo;
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepo;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public CopyCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<CostEstimateGroup> groupRepo,
            IRepository<CostEstimateItem> itemRepo,
            IRepository<CostEstimateAdditionalFieldValue> additionalFieldValueRepo,
            IRepository<CostEstimateFieldSchema> fieldSchemaRepo,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepo = costEstimateRepo;
            this.groupRepo = groupRepo;
            this.itemRepo = itemRepo;
            this.additionalFieldValueRepo = additionalFieldValueRepo;
            this.fieldSchemaRepo = fieldSchemaRepo;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<List<Guid>> Handle(CopyCostEstimateCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid costEstimateId = request.CostEstimateId;

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

            IEnumerable<CostEstimateGroup> sourceGroupsQuery = await groupRepo.GetBySearch(
                g => g.CostEstimateId == costEstimateId && !g.IsDeleted);
            List<CostEstimateGroup> sourceGroups = sourceGroupsQuery.ToList();

            IEnumerable<CostEstimateItem> sourceItemsQuery = await itemRepo.GetBySearch(
                i => i.CostEstimateId == costEstimateId && !i.IsDeleted);
            List<CostEstimateItem> sourceItems = sourceItemsQuery.ToList();

            IEnumerable<CostEstimateAdditionalFieldValue> sourceAdditionalFieldValuesQuery =
                await additionalFieldValueRepo.GetBySearch(
                    v => v.GroupId.HasValue
                        ? sourceGroups.Select(g => g.Id).Contains(v.GroupId!.Value)
                        : sourceItems.Select(i => i.Id).Contains(v.ItemId!.Value));
            List<CostEstimateAdditionalFieldValue> sourceAdditionalFieldValues = sourceAdditionalFieldValuesQuery.ToList();

            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByGroupId =
                sourceAdditionalFieldValues
                    .Where(v => v.GroupId.HasValue)
                    .GroupBy(v => v.GroupId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByItemId =
                sourceAdditionalFieldValues
                    .Where(v => v.ItemId.HasValue)
                    .GroupBy(v => v.ItemId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = sourceItems
                .Where(i => i.ParentItemId == null)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId = sourceItems
                .Where(i => i.ParentItemId != null)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<Guid> createdCostEstimateIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            foreach (Guid targetProjectId in request.TargetProjectIds)
            {
                Guid newCostEstimateId = await CreateCopiedCostEstimateAsync(
                    sourceCostEstimate,
                    tenantId,
                    targetProjectId,
                    sourceGroups,
                    mainItemsByGroupId,
                    childItemsByParentId,
                    additionalFieldValuesByGroupId,
                    additionalFieldValuesByItemId,
                    now,
                    cancellationToken);

                createdCostEstimateIds.Add(newCostEstimateId);
            }

            return createdCostEstimateIds;
        }

        private async Task<Guid> CreateCopiedCostEstimateAsync(
            CostEstimate sourceCostEstimate,
            Guid tenantId,
            Guid targetProjectId,
            List<CostEstimateGroup> sourceGroups,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByGroupId,
            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByItemId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            CostEstimate copiedCostEstimate = new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProjectId = targetProjectId,
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

            IEnumerable<CostEstimateFieldSchema> sourceFieldSchemas = await fieldSchemaRepo.GetBySearch(
                f => f.CostEstimateId == sourceCostEstimate.Id);
            Dictionary<Guid, Guid> fieldSchemaIdMapping = new Dictionary<Guid, Guid>();

            List<CostEstimateFieldSchema> copiedFieldSchemas = sourceFieldSchemas
                .OrderBy(f => f.Order)
                .Select(sourceField =>
                {
                    Guid newFieldId = Guid.NewGuid();
                    fieldSchemaIdMapping[sourceField.Id] = newFieldId;
                    return new CostEstimateFieldSchema
                    {
                        Id = newFieldId,
                        CostEstimateId = copiedCostEstimate.Id,
                        FieldName = sourceField.FieldName,
                        FieldKey = sourceField.FieldKey,
                        FieldType = sourceField.FieldType,
                        IsBasicField = sourceField.IsBasicField,
                        IsAdditionalField = sourceField.IsAdditionalField,
                        Order = sourceField.Order,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                })
                .ToList();

            if (copiedFieldSchemas.Count == 0)
            {
                copiedFieldSchemas = DefaultCostEstimateFieldSchemaFactory.CreateDefaultSchema(
                    copiedCostEstimate.Id,
                    now);
            }

            await fieldSchemaRepo.InsertRange(copiedFieldSchemas);
            await costEstimateRepo.SaveChangesAsync(cancellationToken);

            await CopyGroupsAndItemsAsync(
                copiedCostEstimate.Id,
                sourceGroups,
                mainItemsByGroupId,
                childItemsByParentId,
                additionalFieldValuesByGroupId,
                additionalFieldValuesByItemId,
                fieldSchemaIdMapping,
                now,
                cancellationToken);

            return copiedCostEstimate.Id;
        }

        private async Task CopyGroupsAndItemsAsync(
            Guid copiedCostEstimateId,
            List<CostEstimateGroup> sourceGroups,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByGroupId,
            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByItemId,
            Dictionary<Guid, Guid> fieldSchemaIdMapping,
            DateTime now,
            CancellationToken cancellationToken)
        {
            Dictionary<Guid, Guid> groupIdMapping = new Dictionary<Guid, Guid>();
            List<CostEstimateGroup> allCopiedGroups = new List<CostEstimateGroup>();
            List<CostEstimateItem> allCopiedItems = new List<CostEstimateItem>();
            List<CostEstimateAdditionalFieldValue> allCopiedAdditionalFieldValues = new List<CostEstimateAdditionalFieldValue>();

            foreach (CostEstimateGroup sourceGroup in sourceGroups.OrderBy(g => g.Level))
            {
                Guid newGroupId = Guid.NewGuid();
                groupIdMapping[sourceGroup.Id] = newGroupId;

                allCopiedGroups.Add(new CostEstimateGroup
                {
                    Id = newGroupId,
                    CostEstimateId = copiedCostEstimateId,
                    Name = sourceGroup.Name,
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

                if (additionalFieldValuesByGroupId.TryGetValue(sourceGroup.Id, out List<CostEstimateAdditionalFieldValue>? groupAdditionalValues))
                {
                    foreach (CostEstimateAdditionalFieldValue sourceValue in groupAdditionalValues)
                    {
                        allCopiedAdditionalFieldValues.Add(new CostEstimateAdditionalFieldValue
                        {
                            Id = Guid.NewGuid(),
                            FieldSchemaId = fieldSchemaIdMapping.GetValueOrDefault(
                                sourceValue.FieldSchemaId,
                                sourceValue.FieldSchemaId),
                            GroupId = newGroupId,
                            ItemId = null,
                            StringValue = sourceValue.StringValue,
                            DecimalValue = sourceValue.DecimalValue,
                            BoolValue = sourceValue.BoolValue,
                            DateTimeValue = sourceValue.DateTimeValue,
                            CreatedAt = now
                        });
                    }
                }

                List<CostEstimateItem> mainItems = mainItemsByGroupId.TryGetValue(sourceGroup.Id, out List<CostEstimateItem>? items)
                    ? items
                    : new List<CostEstimateItem>();

                foreach (CostEstimateItem sourceItem in mainItems)
                {
                    CollectCopiedItems(
                        copiedCostEstimateId,
                        newGroupId,
                        null,
                        sourceItem,
                        now,
                        allCopiedItems,
                        allCopiedAdditionalFieldValues,
                        childItemsByParentId,
                        additionalFieldValuesByItemId,
                        fieldSchemaIdMapping);
                }
            }

            await groupRepo.InsertRange(allCopiedGroups);
            await itemRepo.InsertRange(allCopiedItems);
            await additionalFieldValueRepo.InsertRange(allCopiedAdditionalFieldValues);
            await groupRepo.SaveChangesAsync(cancellationToken);
        }

        private static void CollectCopiedItems(
            Guid costEstimateId,
            Guid groupId,
            Guid? parentItemId,
            CostEstimateItem sourceItem,
            DateTime now,
            List<CostEstimateItem> allItems,
            List<CostEstimateAdditionalFieldValue> allAdditionalFieldValues,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            Dictionary<Guid, List<CostEstimateAdditionalFieldValue>> additionalFieldValuesByItemId,
            Dictionary<Guid, Guid> fieldSchemaIdMapping)
        {
            Guid newItemId = Guid.NewGuid();

            allItems.Add(new CostEstimateItem
            {
                Id = newItemId,
                CostEstimateId = costEstimateId,
                GroupId = groupId,
                ParentItemId = parentItemId,
                RelationType = sourceItem.RelationType,
                Order = sourceItem.Order,
                Name = sourceItem.Name,
                Quantity = sourceItem.Quantity,
                Unit = sourceItem.Unit,
                UnitPriceNet = sourceItem.UnitPriceNet,
                VatRate = sourceItem.VatRate,
                UnitPriceGross = sourceItem.UnitPriceGross,
                NetValue = sourceItem.NetValue,
                GrossValue = sourceItem.GrossValue,
                VatValue = sourceItem.VatValue,
                IsSelected = sourceItem.IsSelected,
                IsStageWork = sourceItem.IsStageWork,
                CreatedAt = now,
                IsDeleted = false
            });

            if (additionalFieldValuesByItemId.TryGetValue(sourceItem.Id, out List<CostEstimateAdditionalFieldValue>? sourceAdditionalValues))
            {
                foreach (CostEstimateAdditionalFieldValue sourceValue in sourceAdditionalValues)
                {
                    allAdditionalFieldValues.Add(new CostEstimateAdditionalFieldValue
                    {
                        Id = Guid.NewGuid(),
                        FieldSchemaId = fieldSchemaIdMapping.GetValueOrDefault(
                            sourceValue.FieldSchemaId,
                            sourceValue.FieldSchemaId),
                        GroupId = null,
                        ItemId = newItemId,
                        StringValue = sourceValue.StringValue,
                        DecimalValue = sourceValue.DecimalValue,
                        BoolValue = sourceValue.BoolValue,
                        DateTimeValue = sourceValue.DateTimeValue,
                        CreatedAt = now
                    });
                }
            }

            if (childItemsByParentId.TryGetValue(sourceItem.Id, out List<CostEstimateItem>? sourceChildren))
            {
                foreach (CostEstimateItem sourceChild in sourceChildren)
                {
                    CollectCopiedItems(
                        costEstimateId,
                        groupId,
                        newItemId,
                        sourceChild,
                        now,
                        allItems,
                        allAdditionalFieldValues,
                        childItemsByParentId,
                        additionalFieldValuesByItemId,
                        fieldSchemaIdMapping);
                }
            }
        }
    }
}
