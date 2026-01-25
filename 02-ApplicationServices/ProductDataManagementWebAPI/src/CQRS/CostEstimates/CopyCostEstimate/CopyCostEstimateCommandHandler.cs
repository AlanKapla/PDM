using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
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
        private readonly ICurrentUser currentUser;

        public CopyCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<CostEstimateGroup> groupRepo,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepo,
            IRepository<CostEstimateItem> itemRepo,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepo,
            ICurrentUser currentUser)
        {
            this.costEstimateRepo = costEstimateRepo;
            this.groupRepo = groupRepo;
            this.groupFieldValueRepo = groupFieldValueRepo;
            this.itemRepo = itemRepo;
            this.itemFieldValueRepo = itemFieldValueRepo;
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

            // 2. Authorization check: tenant admin OR project admin OR cost estimate owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, request.ProjectId, cancellationToken);
            bool isOwner = sourceCostEstimate.OwnerId == currentUser.Id;
            
            if (!isAdmin && !isOwner)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

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
                    TemplateVersionId = sourceCostEstimate.TemplateVersionId,
                    OwnerId = currentUser.Id,
                    Name = $"{sourceCostEstimate.Name} (kopia)",
                    Description = sourceCostEstimate.Description,
                    Status = CostEstimateStatus.Draft,
                    SelectedCurrencyId = sourceCostEstimate.SelectedCurrencyId,
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
                
                // Copy all groups (maintain hierarchy)
                foreach (var sourceGroup in sourceCostEstimate.AllGroups.OrderBy(g => g.Level))
                {
                    var newGroupId = Guid.NewGuid();
                    groupIdMapping[sourceGroup.Id] = newGroupId;
                    
                    var copiedGroup = new CostEstimateGroup
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
                    };
                    
                    await groupRepo.Insert(copiedGroup);
                    
                    // Copy group field values
                    foreach (var sourceFieldValue in sourceGroup.FieldValues)
                    {
                        var copiedFieldValue = new CostEstimateGroupFieldValue
                        {
                            Id = Guid.NewGuid(),
                            GroupId = newGroupId,
                            FieldDefinitionId = sourceFieldValue.FieldDefinitionId,
                            Value = sourceFieldValue.Value,
                            CreatedAt = now
                        };
                        
                        await groupFieldValueRepo.Insert(copiedFieldValue);
                    }
                    
                    // Copy work scope items for this group (tylko główne pozycje - ParentItemId == null)
                    var mainItems = sourceGroup.Items.Where(i => i.ParentItemId == null).ToList();
                    var itemIdMapping = new Dictionary<Guid, Guid>(); // old item ID => new item ID
                    
                    foreach (var sourceItem in mainItems)
                    {
                        await CopySingleItemWithOptionsAsync(
                            copiedCostEstimate.Id,
                            newGroupId,
                            null, // ParentItemId = null dla głównych pozycji
                            sourceItem,
                            itemIdMapping,
                            now,
                            cancellationToken);
                    }
                }
                
                // Save all changes for this copy
                await groupRepo.SaveChangesAsync(cancellationToken);
                await groupFieldValueRepo.SaveChangesAsync(cancellationToken);
                await itemRepo.SaveChangesAsync(cancellationToken);
                await itemFieldValueRepo.SaveChangesAsync(cancellationToken);

                createdCostEstimateIds.Add(copiedCostEstimate.Id);
            }

            return createdCostEstimateIds;
        }

        private async Task CopySingleItemWithOptionsAsync(
            Guid costEstimateId,
            Guid groupId,
            Guid? parentItemId,
            CostEstimateItem sourceItem,
            Dictionary<Guid, Guid> itemIdMapping,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var newItemId = Guid.NewGuid();
            itemIdMapping[sourceItem.Id] = newItemId;
            
            var copiedItem = new CostEstimateItem
            {
                Id = newItemId,
                CostEstimateId = costEstimateId,
                GroupId = groupId,
                ParentItemId = parentItemId,
                Order = sourceItem.Order,
                CreatedAt = now,
                IsDeleted = false
            };
            
            await itemRepo.Insert(copiedItem);
            
            // Copy field values
            foreach (var sourceFieldValue in sourceItem.FieldValues)
            {
                var copiedFieldValue = new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = newItemId,
                    FieldDefinitionId = sourceFieldValue.FieldDefinitionId,
                    Value = sourceFieldValue.Value,
                    CreatedAt = now
                };
                
                await itemFieldValueRepo.Insert(copiedFieldValue);
            }
            
            // Rekursywnie skopiuj opcje (jeśli są)
            if (sourceItem.Options != null && sourceItem.Options.Any())
            {
                foreach (var sourceOption in sourceItem.Options.Where(o => !o.IsDeleted))
                {
                    await CopySingleItemWithOptionsAsync(
                        costEstimateId,
                        groupId,
                        newItemId, // ParentItemId dla opcji
                        sourceOption,
                        itemIdMapping,
                        now,
                        cancellationToken);
                }
            }
        }
    }
}
