using Business.Implementation.Validators;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Business.Implementation.Helpers;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Handler dla aktualizacji kosztorysu
    /// Usuwa wszystkie istniejące grupy/pozycje i tworzy nowe według RootGroups
    /// Waliduje strukturę grup i wartości pół przed aktualizacją
    /// Automatycznie przelicza sumy po aktualizacji
    /// </summary>
    public class UpdateCostEstimateCommandHandler : IRequestHandler<UpdateCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly CostEstimateGroupValidator groupValidator;
        private readonly CostEstimateItemValidator itemValidator;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ICostEstimateCalculationService calculationService,
            CostEstimateGroupValidator groupValidator,
            CostEstimateItemValidator itemValidator,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.templateRepository = templateRepository;
            this.groupRepository = groupRepository;
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.itemRepository = itemRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.calculationService = calculationService;
            this.groupValidator = groupValidator;
            this.itemValidator = itemValidator;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            // Get cost estimate first
            var costEstimates = await costEstimateRepository.GetBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted &&
                     c.OwnerId == currentUser.Id,
                q => q.Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.FieldValues)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                              .ThenInclude(w => w.FieldValues)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                              .ThenInclude(w => w.Options.Where(o => !o.IsDeleted))
                                  .ThenInclude(o => o.FieldValues));

            var costEstimate = costEstimates.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // Now get template with version and definitions
            var templates = await templateRepository.GetBySearch(
                t => t.Id == costEstimate.TemplateId,
                q => q
                    .Include(v => v.GroupFieldDefinitions)
                    .Include(v => v.SystemFieldDefinitions)
                    .Include(v => v.CalculatedFieldDefinitions)
                    .Include(v => v.GenericFieldDefinitions));

            var template = templates.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            // ✅ Zbuduj słowniki field definitions raz na początku
            var groupFieldDefinitionsById = template.GroupFieldDefinitions.ToDictionary(f => f.Id);
            var allItemFieldDefinitionsById = template.SystemFieldDefinitions
                .Cast<CostEstimateTemplateFieldDefinitionBase>()
                .Concat(template.CalculatedFieldDefinitions)
                .Concat(template.GenericFieldDefinitions)
                .ToDictionary(f => f.Id);

            // Validate group hierarchy before updating
            var tempGroups = BuildTemporaryGroupsForValidation(request.RootGroups, costEstimate.Id);
            var hierarchyValidation = groupValidator.ValidateGroupHierarchy(
                template,
                tempGroups,
                cancellationToken);

            if (!hierarchyValidation.IsValid)
            {
                throw new ValidationApiException(string.Join("; ", hierarchyValidation.Errors));
            }

            var now = DateTime.UtcNow;

            // ✅ Zrób snapshot istniejących group IDs PRZED update (aby nie uwzględnić nowo dodanych)
            var existingGroupIds = costEstimate.AllGroups
                .Select(g => g.Id)
                .ToHashSet();

            // Update basic properties
            costEstimate.Name = request.Name;
            costEstimate.Description = request.Description;
            costEstimate.Status = request.Status;
            costEstimate.UpdatedAt = now;

            await costEstimateRepository.Update(costEstimate);

            // Strategy: Update/Create/Delete based on Id matching
            await UpdateGroupHierarchyAsync(
                costEstimate.Id,
                groupFieldDefinitionsById,
                allItemFieldDefinitionsById,
                request.RootGroups,
                costEstimate.AllGroups.ToList(),
                null,
                0,
                now,
                cancellationToken);

            // ✅ Delete groups that were in DB but are no longer in request
            // Używamy existingGroupIds (snapshot sprzed update) zamiast costEstimate.AllGroups (tracked!)
            var requestedGroupIds = CollectAllGroupIds(request.RootGroups);
            var groupIdsToDelete = existingGroupIds
                .Where(id => !requestedGroupIds.Contains(id))
                .ToHashSet();

            if (groupIdsToDelete.Any())
            {
                // Pobierz grupy do soft delete z bazy (nie używaj costEstimate.AllGroups - tracked!)
                var groupsToDelete = await groupRepository.GetBySearch(
                    g => groupIdsToDelete.Contains(g.Id) && !g.IsDeleted);

                foreach (var group in groupsToDelete)
                {
                    group.IsDeleted = true;
                    group.DeletedAt = now;
                    await groupRepository.Update(group);
                }

                await groupRepository.SaveChangesAsync(cancellationToken);
            }

            // Reload cost estimate with all groups and items for calculation
            // UWAGA: Options i Components są filtrowane w kodzie przez RelationType!
            var costEstimateForCalculation = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && !c.IsDeleted,
                q => q.Include(c => c.Template)
                        .ThenInclude(t => t.CalculatedFieldDefinitions)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted && w.RelationType == ItemRelationType.None))  // ✅ Tylko główne pozycje
                              .ThenInclude(w => w.FieldValues)
                                  .ThenInclude(fv => fv.FieldDefinition)
                      .Include(c => c.AllItems.Where(i => !i.IsDeleted && i.ParentItemId != null))  // ✅ Wszystkie child items (Options + Components)
                          .ThenInclude(i => i.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition));

            if (costEstimateForCalculation == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }
            
            // ✅ Populate Options i Components dla wszystkich pozycji
            costEstimateForCalculation.PopulateItemHierarchy();

            // Recalculate totals after update
            calculationService.RecalculateCostEstimate(costEstimateForCalculation);

            // Save calculated totals
            await costEstimateRepository.Update(costEstimateForCalculation);
            
            foreach (var group in costEstimateForCalculation.AllGroups.Where(g => !g.IsDeleted))
            {
                await groupRepository.Update(group);
            }
            
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private List<CostEstimateGroup> BuildTemporaryGroupsForValidation(
            List<CostEstimateGroupDto> groupDtos,
            Guid costEstimateId,
            Guid? parentGroupId = null,
            int level = 0)
        {
            var groups = new List<CostEstimateGroup>();

            foreach (var groupDto in groupDtos)
            {
                var groupId = groupDto.Id ?? Guid.NewGuid();
                var group = new CostEstimateGroup
                {
                    Id = groupId,
                    CostEstimateId = costEstimateId,
                    ParentGroupId = parentGroupId,
                    Level = level,
                    Order = groupDto.Order
                };

                groups.Add(group);

                if (groupDto.ChildGroups.Count > 0)
                {
                    var childGroups = BuildTemporaryGroupsForValidation(
                        groupDto.ChildGroups,
                        costEstimateId,
                        groupId,
                        level + 1);
                    groups.AddRange(childGroups);
                }
            }

            return groups;
        }

        private async Task UpdateGroupHierarchyAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> groupFieldDefinitionsById,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            List<CostEstimateGroupDto> groupDtos,
            List<CostEstimateGroup> existingGroups,
            Guid? parentGroupId,
            int level,
            DateTime now,
            CancellationToken cancellationToken)
        {
            foreach (var groupDto in groupDtos)
            {
                CostEstimateGroup group;
                Guid groupId;
                bool isNewGroup = false;

                if (groupDto.Id.HasValue && existingGroups.Any(g => g.Id == groupDto.Id.Value))
                {
                    // Update existing group
                    group = existingGroups.First(g => g.Id == groupDto.Id.Value);
                    groupId = group.Id;

                    group.ParentGroupId = parentGroupId;
                    group.Level = level;
                    group.Order = groupDto.Order;
                    group.UpdatedAt = now;

                    await groupRepository.Update(group);

                    // Delete old field values and create new ones
                    var existingFieldValues = await groupFieldValueRepository.GetBySearch(
                        fv => fv.GroupId == groupId);

                    foreach (var fv in existingFieldValues)
                    {
                        await groupFieldValueRepository.Delete(fv);
                    }
                }
                else
                {
                    // Create new group
                    groupId = Guid.NewGuid();
                    isNewGroup = true;
                    group = new CostEstimateGroup
                    {
                        Id = groupId,
                        CostEstimateId = costEstimateId,
                        ParentGroupId = parentGroupId,
                        Level = level,
                        Order = groupDto.Order,
                        TotalNet = null,
                        TotalGross = null,
                        TotalVat = null,
                        CreatedAt = now,
                        IsDeleted = false
                    };

                    await groupRepository.Insert(group);
                    
                    // ✅ SaveChanges po Insert nowej grupy aby Id było w DB dla FK field values
                    await groupRepository.SaveChangesAsync(cancellationToken);
                }

                // Validate and create field values using dictionary and typed properties
                var fieldValues = groupDto.FieldValues.Select(fv =>
                {
                    // Pobierz definicję pola aby znać FieldType
                    if (!groupFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                    {
                        throw new ValidationApiException($"Field definition {fv.FieldDefinitionId} not found in template");
                    }

                    var fieldValue = new CostEstimateGroupFieldValue
                    {
                        Id = Guid.NewGuid(),
                        GroupId = groupId,
                        FieldDefinitionId = fv.FieldDefinitionId,
                        CreatedAt = now
                    };

                    // Ustaw wartość w odpowiednim polu typowanym
                    FieldValueConverter.SetTypedValue(
                        fieldValue,
                        (int)fieldDef.FieldType,
                        fv.StringValue,
                        fv.DecimalValue,
                        fv.BoolValue,
                        fv.DateTimeValue
                    );

                    return fieldValue;
                }).ToList();

                var fieldValidation = groupValidator.ValidateGroupFieldValues(
                    groupFieldDefinitionsById,
                    fieldValues,
                    cancellationToken);

                if (!fieldValidation.IsValid)
                {
                    throw new ValidationApiException($"Group field validation failed: {string.Join("; ", fieldValidation.Errors)}");
                }

                foreach (var fieldValue in fieldValues)
                {
                    await groupFieldValueRepository.Insert(fieldValue);
                }
                
                // ✅ SaveChanges po field values
                if (fieldValues.Any())
                {
                    await groupFieldValueRepository.SaveChangesAsync(cancellationToken);
                }

                // Update/Create work scope items with validation
                await UpdateItemsAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    groupDto.Items,
                    existingGroups.FirstOrDefault(g => g.Id == groupId)?.Items.ToList() ?? new List<CostEstimateItem>(),
                    now,
                    cancellationToken);

                // Recursively update child groups
                if (groupDto.ChildGroups.Count > 0)
                {
                    await UpdateGroupHierarchyAsync(
                        costEstimateId,
                        groupFieldDefinitionsById,
                        allItemFieldDefinitionsById,
                        groupDto.ChildGroups,
                        existingGroups,
                        groupId,
                        level + 1,
                        now,
                        cancellationToken);
                }
            }
        }

        private async Task UpdateItemsAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            Guid groupId,
            List<CostEstimateItemDto> itemDtos,
            List<CostEstimateItem> existingItems,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var requestedItemIds = itemDtos.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

            var itemsToDelete = existingItems.Where(i => !requestedItemIds.Contains(i.Id)).ToList();
            foreach (var item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
                await itemRepository.Update(item);
            }

            foreach (var itemDto in itemDtos)
            {
                await UpdateSingleItemAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    parentItemId: null,              // ParentItemId = null dla głównych pozycji
                    relationType: ItemRelationType.None,  // ✅ Główne pozycje mają RelationType = None
                    itemDto,
                    existingItems,
                    now,
                    cancellationToken);
            }
        }

        private async Task UpdateSingleItemAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            Guid groupId,
            Guid? parentItemId,
            ItemRelationType relationType,  // ✅ RelationType jako parametr (określany przez backend, nie UI!)
            CostEstimateItemDto itemDto,
            List<CostEstimateItem> existingItems,
            DateTime now,
            CancellationToken cancellationToken)
        {
            CostEstimateItem item;
            Guid itemId;
            bool isNewItem = false;

            if (itemDto.Id.HasValue && existingItems.Any(i => i.Id == itemDto.Id.Value))
            {
                item = existingItems.First(i => i.Id == itemDto.Id.Value);
                itemId = item.Id;

                item.ParentItemId = parentItemId;
                item.RelationType = relationType;  // ✅ UPDATE - użyj parametru, nie DTO!
                item.Order = itemDto.Order;
                item.UpdatedAt = now;

                await itemRepository.Update(item);

                var existingFieldValues = await itemFieldValueRepository.GetBySearch(
                    fv => fv.ItemId == itemId);

                foreach (var fv in existingFieldValues)
                {
                    await itemFieldValueRepository.Delete(fv);
                }
            }
            else
            {
                itemId = Guid.NewGuid();
                isNewItem = true;
                item = new CostEstimateItem
                {
                    Id = itemId,
                    CostEstimateId = costEstimateId,
                    GroupId = groupId,
                    ParentItemId = parentItemId,
                    RelationType = relationType,  // ✅ INSERT - użyj parametru, nie DTO!
                    Order = itemDto.Order,
                    CreatedAt = now,
                    IsDeleted = false
                };

                await itemRepository.Insert(item);
                
                // ✅ SaveChanges po Insert nowego itemu aby Id było w DB dla FK field values
                await itemRepository.SaveChangesAsync(cancellationToken);
            }

            var itemFieldValues = itemDto.FieldValues.Select(fv =>
            {
                // Pobierz definicję pola aby znać FieldType
                if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                {
                    throw new ValidationApiException($"Field definition {fv.FieldDefinitionId} not found in template");
                }

                var fieldValue = new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = fv.FieldDefinitionId,
                    CreatedAt = now
                };

                // Ustaw wartość w odpowiednim polu typowanym
                FieldValueConverter.SetTypedValue(
                    fieldValue,
                    (int)fieldDef.FieldType,
                    fv.StringValue,
                    fv.DecimalValue,
                    fv.BoolValue,
                    fv.DateTimeValue
                );

                return fieldValue;
            }).ToList();

            var itemFieldValidation = itemValidator.ValidateItemFieldValues(
                allItemFieldDefinitionsById,
                itemFieldValues,
                cancellationToken);

            if (!itemFieldValidation.IsValid)
            {
                throw new ValidationApiException($"Work scope item field validation failed: {string.Join("; ", itemFieldValidation.Errors)}");
            }

            foreach (var fieldValue in itemFieldValues)
            {
                await itemFieldValueRepository.Insert(fieldValue);
            }
            
            // ✅ SaveChanges po field values
            if (itemFieldValues.Any())
            {
                await itemFieldValueRepository.SaveChangesAsync(cancellationToken);
            }

            // ✅ Rekurencyjnie obsłuż opcje (jeśli są)
            if (itemDto.Options != null && itemDto.Options.Any())
            {
                // ✅ Walidacja: opcje mogą być tylko w pozycjach głównych (None) lub komponentach (Component)
                // Opcje NIE MOGĄ być w opcjach (Option) - tylko 2 poziomy dozwolone
                if (relationType == ItemRelationType.Option)
                {
                    throw new ValidationApiException(
                        $"Item {itemId}: Options cannot have their own Options. " +
                        $"Maximum nesting: Position → Component → Option.");
                }
                
                // ✅ Walidacja: tylko jedna opcja może mieć Selected = true
                ValidateOnlyOneOptionIsSelected(itemDto.Options, allItemFieldDefinitionsById);

                // ✅ Pobierz istniejące opcje tego itemu (child items z ParentItemId == itemId)
                var existingOptions = await itemRepository.GetBySearch(
                    i => i.ParentItemId == itemId && !i.IsDeleted,
                    q => q.Include(i => i.FieldValues));

                var existingOptionsList = existingOptions.ToList();

                // ✅ Rekurencyjnie przetwórz każdą opcję
                foreach (var optionDto in itemDto.Options)
                {
                    await UpdateSingleItemAsync(
                        costEstimateId,
                        allItemFieldDefinitionsById,
                        groupId,
                        parentItemId: itemId,               // ParentItemId dla opcji
                        relationType: ItemRelationType.Option,  // ✅ Backend określa RelationType = Option
                        optionDto,
                        existingOptionsList,
                        now,
                        cancellationToken);
                }
                
                // ✅ Usuń opcje które nie są już w request
                var requestedOptionIds = itemDto.Options
                    .Where(o => o.Id.HasValue)
                    .Select(o => o.Id!.Value)
                    .ToHashSet();
                
                var optionsToDelete = existingOptionsList
                    .Where(o => !requestedOptionIds.Contains(o.Id))
                    .ToList();
                
                foreach (var option in optionsToDelete)
                {
                    option.IsDeleted = true;
                    option.DeletedAt = now;
                    await itemRepository.Update(option);
                }
                
                // ✅ SaveChanges po soft delete opcji
                if (optionsToDelete.Any())
                {
                    await itemRepository.SaveChangesAsync(cancellationToken);
                }
            }
            
            // ✅ Rekurencyjnie obsłuż komponenty (jeśli są)
            if (itemDto.Components != null && itemDto.Components.Any())
            {
                // ✅ Walidacja: pozycja z komponentami może mieć tylko pola opisowe (System, Generic)
                // Blokujemy tylko pola KALKULOWANE (Calculated) - te są obliczane z komponentów
                var calculatedFields = itemDto.FieldValues
                    .Where(fv => 
                    {
                        if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                        {
                            throw new ValidationApiException(
                                $"Field definition {fv.FieldDefinitionId} not found in template. " +
                                $"Cannot set field that doesn't exist in template.");
                        }
                        
                        // FieldType 200-209 = Calculated fields
                        return fieldDef is CostEstimateTemplateItemCalculatedFieldDefinition;
                    })
                    .ToList();
                
                if (calculatedFields.Any())
                {
                    var fieldNames = string.Join(", ", calculatedFields.Select(f => 
                    {
                        allItemFieldDefinitionsById.TryGetValue(f.FieldDefinitionId, out var def);
                        return def?.Label ?? "Unknown";
                    }));
                    
                    throw new ValidationApiException(
                        $"Item {itemId}: Item with Components cannot have calculated fields. " +
                        $"These fields are auto-calculated from components: {fieldNames}. " +
                        $"You can only set descriptive fields (Name, Description, Unit, Custom fields).");
                }
                
                // ✅ Walidacja: komponenty mogą być tylko w pozycjach głównych (None)
                // Komponenty NIE MOGĄ być w komponentach ani opcjach
                if (relationType != ItemRelationType.None)
                {
                    throw new ValidationApiException(
                        $"Item {itemId}: Only main positions (RelationType=None) can have Components. " +
                        $"Components and Options cannot have their own Components.");
                }

                // ✅ Pobierz istniejące komponenty tego itemu
                var existingComponents = await itemRepository.GetBySearch(
                    i => i.ParentItemId == itemId && i.RelationType == ItemRelationType.Component && !i.IsDeleted,
                    q => q.Include(i => i.FieldValues));

                var existingComponentsList = existingComponents.ToList();

                // ✅ Rekurencyjnie przetwórz każdy komponent
                foreach (var componentDto in itemDto.Components)
                {
                    await UpdateSingleItemAsync(
                        costEstimateId,
                        allItemFieldDefinitionsById,
                        groupId,
                        parentItemId: itemId,                      // ParentItemId dla komponentu
                        relationType: ItemRelationType.Component,  // ✅ Backend określa RelationType = Component
                        componentDto,
                        existingComponentsList,
                        now,
                        cancellationToken);
                }
                
                // ✅ Usuń komponenty które nie są już w request
                var requestedComponentIds = itemDto.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value)
                    .ToHashSet();
                
                var componentsToDelete = existingComponentsList
                    .Where(c => !requestedComponentIds.Contains(c.Id))
                    .ToList();
                
                foreach (var component in componentsToDelete)
                {
                    component.IsDeleted = true;
                    component.DeletedAt = now;
                    await itemRepository.Update(component);
                }
                
                // ✅ SaveChanges po soft delete komponentów
                if (componentsToDelete.Any())
                {
                    await itemRepository.SaveChangesAsync(cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// Waliduje że tylko jedna opcja może mieć Selected = true
        /// </summary>
        private void ValidateOnlyOneOptionIsSelected(
            List<CostEstimateItemDto> options,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById)
        {
            // Znajdź definicję pola ItemSystemSelected (FieldType = 104)
            var selectedFieldDefinition = allItemFieldDefinitionsById.Values
                .FirstOrDefault(f => f.FieldType == FieldType.ItemSystemSelected);
            
            if (selectedFieldDefinition == null)
            {
                return;
            }
            
            int selectedCount = 0;
            
            foreach (var option in options)
            {
                var selectedFieldValue = option.FieldValues
                    .FirstOrDefault(fv => fv.FieldDefinitionId == selectedFieldDefinition.Id);
                
                if (selectedFieldValue?.BoolValue == true)
                {
                    selectedCount++;
                }
            }
            
            if (selectedCount > 1)
            {
                throw new ValidationApiException("Only one option can have Selected field set to true");
            }
        }
        
        /// <summary>
        /// Zbiera wszystkie ID grup z hierarchii (rekurencyjnie)
        /// </summary>
        private HashSet<Guid> CollectAllGroupIds(List<CostEstimateGroupDto> groups)
        {
            var ids = new HashSet<Guid>();

            foreach (var group in groups)
            {
                if (group.Id.HasValue)
                {
                    ids.Add(group.Id.Value);
                }

                if (group.ChildGroups.Count > 0)
                {
                    var childIds = CollectAllGroupIds(group.ChildGroups);
                    foreach (var id in childIds)
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }
    }
}
