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
    public class CopyCostEstimateCommandHandler : IRequestHandler<CopyCostEstimateCommand, List<Guid>>
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

            // 1. Verify source cost estimate exists and belongs to the correct project/tenant
            var costEstimates = await costEstimateRepo.GetBySearch(
                ce => ce.Id == request.CostEstimateId
                    && ce.TenantId == tenantId
                    && ce.ProjectId == request.ProjectId
                    && !ce.IsDeleted,
                q => q.Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                              .ThenInclude(g => g.FieldValues)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                              .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                                  .ThenInclude(w => w.FieldValues)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                              .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                                  .ThenInclude(w => w.Options.Where(o => !o.IsDeleted))
                                      .ThenInclude(o => o.FieldValues));

            CostEstimate? sourceCostEstimate = costEstimates.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, tenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel != CostEstimateAccessLevel.Full)
                throw new ForbiddenApiException("Only the owner or an admin can copy this cost estimate.");

            List<Guid> createdCostEstimateIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            // 3. Create copy for each target project
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
                var groupIdMapping = new Dictionary<Guid, Guid>(); // old ID => new ID
                var allCopiedGroups = new List<CostEstimateGroup>();
                var allCopiedGroupFieldValues = new List<CostEstimateGroupFieldValue>();
                var allCopiedItems = new List<CostEstimateItem>();
                var allCopiedItemFieldValues = new List<CostEstimateItemFieldValue>();

                // Copy all groups (maintain hierarchy)
                foreach (var sourceGroup in sourceCostEstimate.AllGroups.OrderBy(g => g.Level))
                {
                    var newGroupId = Guid.NewGuid();
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
                    foreach (var sourceFieldValue in sourceGroup.FieldValues)
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

                    // Copy work scope items for this group (tylko główne pozycje - ParentItemId == null)
                    var mainItems = sourceGroup.Items.Where(i => i.ParentItemId == null).ToList();
                    var itemIdMapping = new Dictionary<Guid, Guid>(); // old item ID => new item ID

                    foreach (var sourceItem in mainItems)
                    {
                        CollectCopiedItems(
                            copiedCostEstimate.Id,
                            newGroupId,
                            null,
                            sourceItem,
                            itemIdMapping,
                            now,
                            allCopiedItems,
                            allCopiedItemFieldValues);
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
            List<CostEstimateItemFieldValue> allFieldValues)
        {
            var newItemId = Guid.NewGuid();
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
            foreach (var sourceFieldValue in sourceItem.FieldValues)
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

            // Recursively collect options
            if (sourceItem.Options != null)
            {
                foreach (var sourceOption in sourceItem.Options.Where(o => !o.IsDeleted))
                {
                    CollectCopiedItems(
                        costEstimateId,
                        groupId,
                        newItemId,
                        sourceOption,
                        itemIdMapping,
                        now,
                        allItems,
                        allFieldValues);
                }
            }
        }
    }
}
