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
    /// Waliduje strukturę grup i wartości pól przed aktualizacją
    /// Automatycznie przelicza sumy po aktualizacji
    ///
    /// Optymalizacje DB:
    /// - Jeden load początkowy z AllGroups.FieldValues + AllItems.FieldValues (brak N+1)
    /// - Pre-built słowniki z załadowanych danych (bez re-query do DB)
    /// - DeleteRange/InsertRange zamiast per-encji Delete/Insert
    /// - Brak pośrednich SaveChanges w pętlach - jeden SaveChanges po całej hierarchii
    /// - Zmutowane tracked encje zapisywane przez change tracking (bez explicit Update)
    /// - Inwaliacja cache po zapisie
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
        private readonly ICostEstimateCacheService ceCacheService;
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
            ICostEstimateCacheService ceCacheService,
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
            this.ceCacheService = ceCacheService;
            this.groupValidator = groupValidator;
            this.itemValidator = itemValidator;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            // Jeden query - ładuje grupy z FV + WSZYSTKIE pozycje (main/opcje/komponenty) z FV
            // Eliminuje N+1 dla grup i pozycji w kolejnych krokach
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId &&
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted &&
                     c.OwnerId == currentUser.Id,
                q => q.Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.FieldValues)
                      .Include(c => c.AllItems.Where(i => !i.IsDeleted))
                          .ThenInclude(i => i.FieldValues))
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == costEstimate.TemplateId,
                q => q
                    .Include(v => v.GroupFieldDefinitions)
                    .Include(v => v.SystemFieldDefinitions)
                    .Include(v => v.CalculatedFieldDefinitions)
                    .Include(v => v.GenericFieldDefinitions))
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            // Słowniki definicji pól - budowane raz na początku
            var groupFieldDefinitionsById = template.GroupFieldDefinitions.ToDictionary(f => f.Id);
            var allItemFieldDefinitionsById = template.SystemFieldDefinitions
                .Cast<CostEstimateTemplateFieldDefinitionBase>()
                .Concat(template.CalculatedFieldDefinitions)
                .Concat(template.GenericFieldDefinitions)
                .ToDictionary(f => f.Id);

            // Walidacja hierarchii grup przed aktualizacją
            var tempGroups = BuildTemporaryGroupsForValidation(request.RootGroups, costEstimate.Id);
            var hierarchyValidation = groupValidator.ValidateGroupHierarchy(template, tempGroups, cancellationToken);
            if (!hierarchyValidation.IsValid)
                throw new ValidationApiException(string.Join("; ", hierarchyValidation.Errors));

            var now = DateTime.UtcNow;

            // Pre-built słowniki z załadowanych danych - zero dodatkowych zapytań do DB
            var allExistingGroupsById = costEstimate.AllGroups.ToDictionary(g => g.Id);
            var allExistingItemsById = costEstimate.AllItems.ToDictionary(i => i.Id);

            // Snapshot ID grup PRZED update (aby poprawnie wykryć grupy do usunięcia)
            var existingGroupIds = allExistingGroupsById.Keys.ToHashSet();

            // Aktualizacja właściwości bazowych
            costEstimate.Name = request.Name;
            costEstimate.Description = request.Description;
            costEstimate.Status = request.Status;
            costEstimate.UpdatedAt = now;

            // Aktualizacja hierarchii grup - używa załadowanych danych, brak dodatkowych DB queries
            await UpdateGroupHierarchyAsync(
                costEstimate.Id,
                groupFieldDefinitionsById,
                allItemFieldDefinitionsById,
                request.RootGroups,
                allExistingGroupsById,
                allExistingItemsById,
                null,
                0,
                now,
                cancellationToken);

            // Soft-delete grup nieobecnych w request - z załadowanych danych, bez DB query
            var requestedGroupIds = CollectAllGroupIds(request.RootGroups);
            var groupsToDelete = allExistingGroupsById.Values
                .Where(g => !requestedGroupIds.Contains(g.Id))
                .ToList();

            foreach (var g in groupsToDelete)
            {
                g.IsDeleted = true;
                g.DeletedAt = now;
                // Tracked entity - EF change tracking wykrywa mutację automatycznie
            }

            // Jeden SaveChanges dla całej hierarchii (grupy + pozycje + wartości pól)
            // EF Core przetwarza DELETE przed INSERT - brak naruszeń unique constraint
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            // Przeładowanie do kalkulacji z nowym stanem DB
            var costEstimateForCalculation = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && !c.IsDeleted,
                q => q.Include(c => c.Template)
                        .ThenInclude(t => t.CalculatedFieldDefinitions)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted && w.RelationType == ItemRelationType.None))
                              .ThenInclude(w => w.FieldValues)
                                  .ThenInclude(fv => fv.FieldDefinition)
                      .Include(c => c.AllItems.Where(i => !i.IsDeleted && i.ParentItemId != null))
                          .ThenInclude(i => i.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition))
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // Populate Options/Components hierarchy
            costEstimateForCalculation.PopulateItemHierarchy();

            // Przelicz sumy
            calculationService.RecalculateCostEstimate(costEstimateForCalculation);

            // NIE wywołuj Update/UpdateRange po PopulateItemHierarchy - powoduje duplicate key errors
            // EF change tracking wykrywa mutacje na tracked encjach automatycznie
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            // Inwaliacja cache po zapisie
            await ceCacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

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
                    groups.AddRange(BuildTemporaryGroupsForValidation(
                        groupDto.ChildGroups, costEstimateId, groupId, level + 1));
                }
            }

            return groups;
        }

        /// <summary>
        /// Aktualizuje hierarchię grup rekurencyjnie.
        /// Używa pre-loaded słowników - brak dodatkowych zapytań do DB.
        /// </summary>
        private async Task UpdateGroupHierarchyAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> groupFieldDefinitionsById,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            List<CostEstimateGroupDto> groupDtos,
            Dictionary<Guid, CostEstimateGroup> allExistingGroupsById,
            Dictionary<Guid, CostEstimateItem> allExistingItemsById,
            Guid? parentGroupId,
            int level,
            DateTime now,
            CancellationToken cancellationToken)
        {
            foreach (var groupDto in groupDtos)
            {
                CostEstimateGroup group;
                Guid groupId;

                if (groupDto.Id.HasValue && allExistingGroupsById.TryGetValue(groupDto.Id.Value, out var existingGroup))
                {
                    // Aktualizacja istniejącej grupy (tracked entity)
                    group = existingGroup;
                    groupId = group.Id;

                    group.ParentGroupId = parentGroupId;
                    group.Level = level;
                    group.Order = groupDto.Order;
                    group.UpdatedAt = now;
                    // Tracked entity - EF change tracking wykrywa mutację, nie wywołujemy Update()

                    // Usuń stare wartości pól - używamy załadowanych danych (brak DB query)
                    // DeleteRange zamiast per-encji Delete
                    if (group.FieldValues.Count > 0)
                    {
                        await groupFieldValueRepository.DeleteRange(group.FieldValues.ToList());
                    }
                }
                else
                {
                    // Nowa grupa
                    groupId = Guid.NewGuid();
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
                    // Brak SaveChanges - EF Core rozwiązuje kolejność FK w końcowym SaveChanges
                }

                // Walidacja i tworzenie wartości pól
                var fieldValues = groupDto.FieldValues.Select(fv =>
                {
                    if (!groupFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                        throw new ValidationApiException($"Field definition {fv.FieldDefinitionId} not found in template");

                    var fieldValue = new CostEstimateGroupFieldValue
                    {
                        Id = Guid.NewGuid(),
                        GroupId = groupId,
                        FieldDefinitionId = fv.FieldDefinitionId,
                        CreatedAt = now
                    };

                    FieldValueConverter.SetTypedValue(
                        fieldValue, (int)fieldDef.FieldType,
                        fv.StringValue, fv.DecimalValue, fv.BoolValue, fv.DateTimeValue);

                    return fieldValue;
                }).ToList();

                var fieldValidation = groupValidator.ValidateGroupFieldValues(
                    groupFieldDefinitionsById, fieldValues, cancellationToken);

                if (!fieldValidation.IsValid)
                    throw new ValidationApiException($"Group field validation failed: {string.Join("; ", fieldValidation.Errors)}");

                // InsertRange zamiast per-encji Insert
                if (fieldValues.Count > 0)
                {
                    await groupFieldValueRepository.InsertRange(fieldValues);
                    // Brak SaveChanges - EF Core przetwarza DELETE przed INSERT w SaveChanges
                    // (brak naruszenia unique constraint na (GroupId, FieldDefinitionId))
                }

                // Aktualizacja pozycji dla tej grupy
                await UpdateItemsAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    groupDto.Items,
                    allExistingItemsById,
                    now,
                    cancellationToken);

                // Rekurencja dla podgrup
                if (groupDto.ChildGroups.Count > 0)
                {
                    await UpdateGroupHierarchyAsync(
                        costEstimateId,
                        groupFieldDefinitionsById,
                        allItemFieldDefinitionsById,
                        groupDto.ChildGroups,
                        allExistingGroupsById,
                        allExistingItemsById,
                        groupId,
                        level + 1,
                        now,
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Aktualizuje główne pozycje dla grupy.
        /// Używa słownika pre-loaded pozycji - brak dodatkowych zapytań do DB.
        /// </summary>
        private async Task UpdateItemsAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            Guid groupId,
            List<CostEstimateItemDto> itemDtos,
            Dictionary<Guid, CostEstimateItem> allExistingItemsById,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var requestedItemIds = itemDtos
                .Where(i => i.Id.HasValue)
                .Select(i => i.Id!.Value)
                .ToHashSet();

            // Soft-delete głównych pozycji nieobecnych w request (tracked entities)
            var mainItemsToDelete = allExistingItemsById.Values
                .Where(i => i.GroupId == groupId &&
                            i.RelationType == ItemRelationType.None &&
                            !requestedItemIds.Contains(i.Id))
                .ToList();

            foreach (var item in mainItemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
                // Tracked entity - EF change tracking wykrywa mutację automatycznie
            }

            foreach (var itemDto in itemDtos)
            {
                await UpdateSingleItemAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    parentItemId: null,
                    relationType: ItemRelationType.None,
                    itemDto,
                    allExistingItemsById,
                    now,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Aktualizuje pojedynczą pozycję (rekurencyjnie obsługuje opcje i komponenty).
        /// Używa słownika pre-loaded pozycji - brak dodatkowych zapytań do DB.
        /// </summary>
        private async Task UpdateSingleItemAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            Guid groupId,
            Guid? parentItemId,
            ItemRelationType relationType,
            CostEstimateItemDto itemDto,
            Dictionary<Guid, CostEstimateItem> allExistingItemsById,
            DateTime now,
            CancellationToken cancellationToken)
        {
            CostEstimateItem item;
            Guid itemId;

            if (itemDto.Id.HasValue && allExistingItemsById.TryGetValue(itemDto.Id.Value, out var existingItem))
            {
                // Aktualizacja istniejącej pozycji (tracked entity)
                item = existingItem;
                itemId = item.Id;

                item.ParentItemId = parentItemId;
                item.RelationType = relationType;
                item.Order = itemDto.Order;
                item.UpdatedAt = now;
                // Tracked entity - EF change tracking wykrywa mutację, nie wywołujemy Update()

                // Usuń stare wartości pól - używamy załadowanych FieldValues (brak DB query)
                // DeleteRange zamiast per-encji Delete
                if (item.FieldValues.Count > 0)
                {
                    await itemFieldValueRepository.DeleteRange(item.FieldValues.ToList());
                }
            }
            else
            {
                // Nowa pozycja
                itemId = Guid.NewGuid();
                item = new CostEstimateItem
                {
                    Id = itemId,
                    CostEstimateId = costEstimateId,
                    GroupId = groupId,
                    ParentItemId = parentItemId,
                    RelationType = relationType,
                    Order = itemDto.Order,
                    CreatedAt = now,
                    IsDeleted = false
                };

                await itemRepository.Insert(item);
                // Brak SaveChanges - EF Core rozwiązuje kolejność FK w końcowym SaveChanges
            }

            // Tworzenie nowych wartości pól
            var itemFieldValues = itemDto.FieldValues.Select(fv =>
            {
                if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                    throw new ValidationApiException($"Field definition {fv.FieldDefinitionId} not found in template");

                var fieldValue = new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = fv.FieldDefinitionId,
                    CreatedAt = now
                };

                FieldValueConverter.SetTypedValue(
                    fieldValue, (int)fieldDef.FieldType,
                    fv.StringValue, fv.DecimalValue, fv.BoolValue, fv.DateTimeValue);

                return fieldValue;
            }).ToList();

            // Walidacja wartości pól
            var itemFieldValidation = itemValidator.ValidateItemFieldValues(
                allItemFieldDefinitionsById, itemFieldValues, cancellationToken);

            if (!itemFieldValidation.IsValid)
                throw new ValidationApiException($"Work scope item field validation failed: {string.Join("; ", itemFieldValidation.Errors)}");

            ValidateFieldRange(itemFieldValues, allItemFieldDefinitionsById, FieldType.ItemCalculatedVatRate, "VatRate");

            // InsertRange zamiast per-encji Insert
            if (itemFieldValues.Count > 0)
            {
                await itemFieldValueRepository.InsertRange(itemFieldValues);
                // Brak SaveChanges - EF Core obsługuje FK ordering w końcowym SaveChanges
            }

            // Rekurencyjne przetwarzanie opcji
            if (itemDto.Options != null && itemDto.Options.Count > 0)
            {
                if (relationType == ItemRelationType.Option)
                    throw new ValidationApiException(
                        $"Item {itemId}: Options cannot have their own Options. " +
                        $"Maximum nesting: Position → Component → Option.");

                ValidateOnlyOneOptionIsSelected(itemDto.Options, allItemFieldDefinitionsById);

                // Filtrujemy istniejące opcje z pre-loaded słownika - brak DB query
                var existingOptionIds = allExistingItemsById.Values
                    .Where(i => i.ParentItemId == itemId && i.RelationType == ItemRelationType.Option)
                    .Select(i => i.Id)
                    .ToHashSet();

                foreach (var optionDto in itemDto.Options)
                {
                    await UpdateSingleItemAsync(
                        costEstimateId, allItemFieldDefinitionsById, groupId,
                        parentItemId: itemId,
                        relationType: ItemRelationType.Option,
                        optionDto, allExistingItemsById, now, cancellationToken);
                }

                // Soft-delete opcji nieobecnych w request
                var requestedOptionIds = itemDto.Options
                    .Where(o => o.Id.HasValue)
                    .Select(o => o.Id!.Value)
                    .ToHashSet();

                foreach (var optionId in existingOptionIds.Where(id => !requestedOptionIds.Contains(id)))
                {
                    if (allExistingItemsById.TryGetValue(optionId, out var optionToDelete))
                    {
                        optionToDelete.IsDeleted = true;
                        optionToDelete.DeletedAt = now;
                        // Tracked entity - EF change tracking wykrywa mutację automatycznie
                    }
                }
            }

            // Rekurencyjne przetwarzanie komponentów
            if (itemDto.Components != null && itemDto.Components.Count > 0)
            {
                // Walidacja: pozycja z komponentami nie może mieć pól kalkulowanych
                var calculatedFields = itemDto.FieldValues
                    .Where(fv =>
                    {
                        if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                            throw new ValidationApiException(
                                $"Field definition {fv.FieldDefinitionId} not found in template. " +
                                $"Cannot set field that doesn't exist in template.");

                        return fieldDef is CostEstimateTemplateItemCalculatedFieldDefinition;
                    })
                    .ToList();

                if (calculatedFields.Count > 0)
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

                if (relationType != ItemRelationType.None)
                    throw new ValidationApiException(
                        $"Item {itemId}: Only main positions (RelationType=None) can have Components. " +
                        $"Components and Options cannot have their own Components.");

                // Filtrujemy istniejące komponenty z pre-loaded słownika - brak DB query
                var existingComponentIds = allExistingItemsById.Values
                    .Where(i => i.ParentItemId == itemId && i.RelationType == ItemRelationType.Component)
                    .Select(i => i.Id)
                    .ToHashSet();

                foreach (var componentDto in itemDto.Components)
                {
                    await UpdateSingleItemAsync(
                        costEstimateId, allItemFieldDefinitionsById, groupId,
                        parentItemId: itemId,
                        relationType: ItemRelationType.Component,
                        componentDto, allExistingItemsById, now, cancellationToken);
                }

                // Soft-delete komponentów nieobecnych w request
                var requestedComponentIds = itemDto.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value)
                    .ToHashSet();

                foreach (var componentId in existingComponentIds.Where(id => !requestedComponentIds.Contains(id)))
                {
                    if (allExistingItemsById.TryGetValue(componentId, out var componentToDelete))
                    {
                        componentToDelete.IsDeleted = true;
                        componentToDelete.DeletedAt = now;
                        // Tracked entity - EF change tracking wykrywa mutację automatycznie
                    }
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
            var selectedFieldDefinition = allItemFieldDefinitionsById.Values
                .FirstOrDefault(f => f.FieldType == FieldType.ItemSystemSelected);

            if (selectedFieldDefinition == null)
                return;

            int selectedCount = 0;

            foreach (var option in options)
            {
                var selectedFieldValue = option.FieldValues
                    .FirstOrDefault(fv => fv.FieldDefinitionId == selectedFieldDefinition.Id);

                if (selectedFieldValue?.BoolValue == true)
                    selectedCount++;
            }

            if (selectedCount > 1)
                throw new ValidationApiException("Only one option can have Selected field set to true");
        }

        /// <summary>
        /// Waliduje że wartość pola mieści się w zakresie 0–1
        /// </summary>
        private static void ValidateFieldRange(
            List<CostEstimateItemFieldValue> fieldValues,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById,
            FieldType targetFieldType,
            string fieldLabel)
        {
            foreach (var fv in fieldValues)
            {
                if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                    continue;

                if (fieldDef.FieldType != targetFieldType)
                    continue;

                if (fv.DecimalValue.HasValue && (fv.DecimalValue.Value < 0m || fv.DecimalValue.Value > 1m))
                    throw new ValidationApiException(
                        $"{fieldLabel} value must be between 0 and 1. Provided: {fv.DecimalValue.Value}");
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
                    ids.Add(group.Id.Value);

                if (group.ChildGroups.Count > 0)
                    ids.UnionWith(CollectAllGroupIds(group.ChildGroups));
            }

            return ids;
        }
    }
}
