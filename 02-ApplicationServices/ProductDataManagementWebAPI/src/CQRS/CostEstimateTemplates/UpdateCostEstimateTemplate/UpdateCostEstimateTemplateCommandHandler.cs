using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public class UpdateCostEstimateTemplateCommandHandler : IRequestHandler<UpdateCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateCurrency> currencyRepository;
        private readonly IRepository<CostEstimateTemplateUnit> unitRepository;
        private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository;
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateCurrency> currencyRepository,
            IRepository<CostEstimateTemplateUnit> unitRepository,
            IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
            IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository,
            IRepository<CostEstimate> costEstimateRepository,
            ICostEstimateCalculationService calculationService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currencyRepository = currencyRepository;
            this.unitRepository = unitRepository;
            this.groupFieldRepository = groupFieldRepository;
            this.systemFieldRepository = systemFieldRepository;
            this.calculatedFieldRepository = calculatedFieldRepository;
            this.genericFieldRepository = genericFieldRepository;
            this.costEstimateRepository = costEstimateRepository;
            this.calculationService = calculationService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted);
            
            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            template.Name = request.Name;
            template.Description = request.Description;
            template.Category = request.Category;
            template.CanAddGroups = request.CanAddGroups;
            template.CanBranchGroups = request.CanBranchGroups;
            template.MaxGroupLevel = request.MaxGroupLevel;
            template.AutoNumberGroups = request.AutoNumberGroups;
            template.GroupNumberFormat = request.GroupNumberFormat;
            template.UpdatedAt = now;

            await templateRepository.Update(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            if (request.Currencies != null)
            {
                await UpdateCurrenciesAsync(template.Id, request.Currencies, cancellationToken);
            }

            if (request.Units != null)
            {
                await UpdateUnitsAsync(template.Id, request.Units, cancellationToken);
            }

            if (!request.UpdateStructure)
            {
                return Unit.Value;
            }

            await DeleteRemovedFieldsAsync(
                template.Id, 
                request.GroupHeaderFields,
                request.SystemFields,
                request.CalculatedFields,
                request.GenericFields,
                cancellationToken);

            var fieldNameToIdMap = new Dictionary<Guid, Guid>();
            
            var columnLayoutOrderMap = BuildColumnLayoutOrderMap(request.UiConfiguration?.ColumnLayout);

            if (request.GroupHeaderFields != null)
            {
                await UpsertFieldsInBatchAsync(
                    request.GroupHeaderFields,
                    template.Id,
                    FieldScope.Group,
                    fieldNameToIdMap,
                    columnLayoutOrderMap,
                    cancellationToken);
            }

            if (request.SystemFields != null)
            {
                await UpsertFieldsInBatchAsync(
                    request.SystemFields,
                    template.Id,
                    FieldScope.ItemSystem,
                    fieldNameToIdMap,
                    columnLayoutOrderMap,
                    cancellationToken);
            }

            if (request.CalculatedFields != null)
            {
                await UpsertFieldsInBatchAsync(
                    request.CalculatedFields,
                    template.Id,
                    FieldScope.ItemCalculated,
                    fieldNameToIdMap,
                    columnLayoutOrderMap,
                    cancellationToken);
            }

            if (request.GenericFields != null)
            {
                await UpsertFieldsInBatchAsync(
                    request.GenericFields,
                    template.Id,
                    FieldScope.ItemGeneric,
                    fieldNameToIdMap,
                    columnLayoutOrderMap,
                    cancellationToken);
            }

            await RecalculateAllCostEstimatesForTemplate(template.Id, cancellationToken);

            return Unit.Value;
        }
        
        
        /// <summary>
        /// Aktualizuje waluty dla szablonu (update/insert)
        /// </summary>
        private async Task UpdateCurrenciesAsync(
            Guid templateId, 
            List<CurrencyDto> currencies, 
            CancellationToken cancellationToken)
        {
            var existingCurrencies = (await currencyRepository
                .GetBySearch(c => c.TemplateId == templateId)).ToList();
            
            var toUpdate = new List<CostEstimateTemplateCurrency>();
            var toInsert = new List<CostEstimateTemplateCurrency>();
            
            foreach (var currencyDto in currencies)
            {
                var existing = existingCurrencies.FirstOrDefault(c => c.Code == currencyDto.Code);
                
                if (existing != null)
                {
                    existing.Name = currencyDto.Name;
                    existing.Symbol = currencyDto.Symbol;
                    existing.IsDefault = currencyDto.IsDefault;
                    existing.Order = currencyDto.Order;
                    toUpdate.Add(existing);
                }
                else
                {
                    toInsert.Add(new CostEstimateTemplateCurrency
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = templateId,
                        Code = currencyDto.Code,
                        Name = currencyDto.Name,
                        Symbol = currencyDto.Symbol,
                        IsDefault = currencyDto.IsDefault,
                        Order = currencyDto.Order
                    });
                }
            }
            
            if (toUpdate.Any())
                await currencyRepository.UpdateRange(toUpdate);
            if (toInsert.Any())
                await currencyRepository.InsertRange(toInsert);
            
            await currencyRepository.SaveChangesAsync(cancellationToken);
        }
        
        
        /// <summary>
        /// Aktualizuje jednostki dla szablonu (update/insert)
        /// </summary>
        private async Task UpdateUnitsAsync(
            Guid templateId, 
            List<UnitDto> units, 
            CancellationToken cancellationToken)
        {
            var existingUnits = (await unitRepository
                .GetBySearch(u => u.TemplateId == templateId)).ToList();
            
            var toUpdate = new List<CostEstimateTemplateUnit>();
            var toInsert = new List<CostEstimateTemplateUnit>();
            
            foreach (var unitDto in units)
            {
                var existing = existingUnits.FirstOrDefault(u => u.Code == unitDto.Code);
                
                if (existing != null)
                {
                    existing.Name = unitDto.Name;
                    existing.Symbol = unitDto.Symbol;
                    existing.Category = unitDto.Category;
                    existing.IsDefault = unitDto.IsDefault;
                    existing.Order = unitDto.Order;
                    toUpdate.Add(existing);
                }
                else
                {
                    toInsert.Add(new CostEstimateTemplateUnit
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = templateId,
                        Code = unitDto.Code,
                        Name = unitDto.Name,
                        Symbol = unitDto.Symbol,
                        Category = unitDto.Category,
                        IsDefault = unitDto.IsDefault,
                        Order = unitDto.Order
                    });
                }
            }
            
            if (toUpdate.Any())
                await unitRepository.UpdateRange(toUpdate);
            if (toInsert.Any())
                await unitRepository.InsertRange(toInsert);
            
            await unitRepository.SaveChangesAsync(cancellationToken);
        }
        
        /// <summary>
        /// Buduje mapę FieldName -> Order na podstawie UiConfiguration.ColumnLayout
        /// </summary>
        private Dictionary<Guid, int> BuildColumnLayoutOrderMap(List<Guid>? columnLayout)
        {
            var orderMap = new Dictionary<Guid, int>();
            
            if (columnLayout == null || !columnLayout.Any())
            {
                return orderMap;
            }
            
            for (int i = 0; i < columnLayout.Count; i++)
            {
                orderMap[columnLayout[i]] = i;
            }
            
            return orderMap;
        }
        
        /// <summary>
        /// Usuwa tylko te pola, które zostały usunięte z requestu (nie są w nowej liście)
        /// Dzięki temu zachowujemy wartości pól (FieldValues) dla niezmienonych/zaktualizowanych pól
        /// </summary>
        private async Task DeleteRemovedFieldsAsync(
            Guid templateId,
            List<FieldDefinitionDto>? newGroupFields,
            List<FieldDefinitionDto>? newSystemFields,
            List<FieldDefinitionDto>? newCalculatedFields,
            List<FieldDefinitionDto>? newGenericFields,
            CancellationToken cancellationToken)
        {
            // Zbierz wszystkie FieldName z nowych pól (rekurencyjnie z child fields)
            var newFieldNames = new HashSet<Guid>();
            
            CollectFieldNames(newGroupFields, newFieldNames);
            CollectFieldNames(newSystemFields, newFieldNames);
            CollectFieldNames(newCalculatedFields, newFieldNames);
            CollectFieldNames(newGenericFields, newFieldNames);
            
            // Usuń tylko te pola, których FieldName NIE MA w nowej liście
            await DeleteFieldsNotInSet(groupFieldRepository, templateId, newFieldNames, cancellationToken);
            await DeleteFieldsNotInSet(systemFieldRepository, templateId, newFieldNames, cancellationToken);
            await DeleteFieldsNotInSet(calculatedFieldRepository, templateId, newFieldNames, cancellationToken);
            await DeleteFieldsNotInSet(genericFieldRepository, templateId, newFieldNames, cancellationToken);
        }
        
        /// <summary>
        /// Rekurencyjnie zbiera wszystkie FieldName z listy pól (włącznie z child fields)
        /// </summary>
        private void CollectFieldNames(List<FieldDefinitionDto>? fields, HashSet<Guid> fieldNames)
        {
            if (fields == null) return;
            
            foreach (var field in fields)
            {
                fieldNames.Add(field.FieldName);
                
                if (field.ChildFields != null && field.ChildFields.Any())
                {
                    CollectFieldNames(field.ChildFields, fieldNames);
                }
            }
        }
        
        /// <summary>
        /// Usuwa pola z repozytorium, których FieldName NIE MA w podanym zestawie
        /// Usuwa najpierw child fields (ParentFieldId != null), potem parent fields (ParentFieldId == null)
        /// aby uniknąć konfliktu FK constraint
        /// </summary>
        private async Task DeleteFieldsNotInSet<T>(
            IRepository<T> repository,
            Guid templateId,
            HashSet<Guid> keepFieldNames,
            CancellationToken cancellationToken) where T : CostEstimateTemplateFieldDefinitionBase
        {
            var existingFields = await repository.GetBySearch(f => f.TemplateId == templateId);
            
            var fieldsToDelete = existingFields
                .Where(f => !keepFieldNames.Contains(f.FieldName))
                .ToList();
            
            if (!fieldsToDelete.Any())
            {
                return;
            }
            
            // Usuń najpierw child fields (mają ParentFieldId)
            var childFieldsToDelete = fieldsToDelete
                .Where(f => f.ParentFieldId.HasValue)
                .ToList();
            
            if (childFieldsToDelete.Any())
            {
                await repository.DeleteRange(childFieldsToDelete);
                await repository.SaveChangesAsync(cancellationToken);
            }
            
            // Potem usuń parent fields (nie mają ParentFieldId)
            var parentFieldsToDelete = fieldsToDelete
                .Where(f => !f.ParentFieldId.HasValue)
                .ToList();
            
            if (parentFieldsToDelete.Any())
            {
                await repository.DeleteRange(parentFieldsToDelete);
                await repository.SaveChangesAsync(cancellationToken);
            }
        }
        
        
        
        
        /// <summary>
        /// Aktualizuje lub tworzy pola w batch (z hierarchią)
        /// Jeśli pole (po FieldName) już istnieje - aktualizuje, jeśli nie - tworzy nowe
        /// </summary>
        private async Task UpsertFieldsInBatchAsync(
            List<FieldDefinitionDto> fieldDtos,
            Guid templateId,
            FieldScope fieldScope,
            Dictionary<Guid, Guid> fieldNameToIdMap,
            Dictionary<Guid, int> columnLayoutOrderMap,
            CancellationToken cancellationToken)
        {
            // Pobierz wszystkie istniejące pola (wszystkie scope - dla child fields)
            var allExistingFields = new Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase>();
            
            foreach (var scope in Enum.GetValues<FieldScope>())
            {
                var scopeFields = await GetExistingFieldsByScope(templateId, scope);
                foreach (var field in scopeFields)
                {
                    allExistingFields[field.FieldName] = field;
                }
            }
            
            // Kolekcje zgrupowane po rzeczywistym FieldScope
            var fieldsToInsertByScope = new Dictionary<FieldScope, List<CostEstimateTemplateFieldDefinitionBase>>();
            var fieldsToUpdateByScope = new Dictionary<FieldScope, List<CostEstimateTemplateFieldDefinitionBase>>();
            
            foreach (var fieldDto in fieldDtos)
            {
                CollectFieldsForUpsert(
                    fieldDto,
                    templateId,
                    fieldScope,
                    parentFieldId: null,
                    orderInParent: null,
                    fieldsToInsertByScope,
                    fieldsToUpdateByScope,
                    allExistingFields,
                    fieldNameToIdMap,
                    columnLayoutOrderMap);
            }
            
            // Update existing - dla każdego scope osobno
            foreach (var kvp in fieldsToUpdateByScope)
            {
                if (kvp.Value.Any())
                {
                    await UpdateFieldsByScope(kvp.Key, kvp.Value, cancellationToken);
                }
            }
            
            // Insert new - dla każdego scope osobno
            foreach (var kvp in fieldsToInsertByScope)
            {
                if (kvp.Value.Any())
                {
                    await InsertFieldsByScope(kvp.Key, kvp.Value, cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// Pobiera istniejące pola dla danego scope
        /// </summary>
        private async Task<List<CostEstimateTemplateFieldDefinitionBase>> GetExistingFieldsByScope(
            Guid templateId, 
            FieldScope fieldScope)
        {
            return fieldScope switch
            {
                FieldScope.Group => (await groupFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                    .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
                FieldScope.ItemSystem => (await systemFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                    .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
                FieldScope.ItemCalculated => (await calculatedFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                    .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
                FieldScope.ItemGeneric => (await genericFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                    .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
                _ => new List<CostEstimateTemplateFieldDefinitionBase>()
            };
        }
        
        /// <summary>
        /// Aktualizuje pola dla danego scope
        /// </summary>
        private async Task UpdateFieldsByScope(
            FieldScope fieldScope,
            List<CostEstimateTemplateFieldDefinitionBase> fields,
            CancellationToken cancellationToken)
        {
            switch (fieldScope)
            {
                case FieldScope.Group:
                    await groupFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateGroupFieldDefinition>());
                    break;
                case FieldScope.ItemSystem:
                    await systemFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemSystemFieldDefinition>());
                    break;
                case FieldScope.ItemCalculated:
                    await calculatedFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemCalculatedFieldDefinition>());
                    break;
                case FieldScope.ItemGeneric:
                    await genericFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemGenericFieldDefinition>());
                    break;
            }
            
            await groupFieldRepository.SaveChangesAsync(cancellationToken);
        }
        
        /// <summary>
        /// Wstawia nowe pola dla danego scope
        /// </summary>
        private async Task InsertFieldsByScope(
            FieldScope fieldScope,
            List<CostEstimateTemplateFieldDefinitionBase> fields,
            CancellationToken cancellationToken)
        {
            switch (fieldScope)
            {
                case FieldScope.Group:
                    await groupFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateGroupFieldDefinition>());
                    break;
                case FieldScope.ItemSystem:
                    await systemFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemSystemFieldDefinition>());
                    break;
                case FieldScope.ItemCalculated:
                    await calculatedFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemCalculatedFieldDefinition>());
                    break;
                case FieldScope.ItemGeneric:
                    await genericFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemGenericFieldDefinition>());
                    break;
            }
            
            await groupFieldRepository.SaveChangesAsync(cancellationToken);
        }
        
        
        /// <summary>
        /// Rekurencyjnie zbiera pola do upsert (update lub insert) i buduje mapę FieldName -> Id
        /// Grupuje pola według ich rzeczywistego FieldScope (nie scope rodzica)
        /// </summary>
        private void CollectFieldsForUpsert(
            FieldDefinitionDto fieldDto,
            Guid templateId,
            FieldScope fieldScope,
            Guid? parentFieldId,
            int? orderInParent,
            Dictionary<FieldScope, List<CostEstimateTemplateFieldDefinitionBase>> fieldsToInsertByScope,
            Dictionary<FieldScope, List<CostEstimateTemplateFieldDefinitionBase>> fieldsToUpdateByScope,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allExistingFields,
            Dictionary<Guid, Guid> fieldNameToIdMap,
            Dictionary<Guid, int> columnLayoutOrderMap)
        {
            // Sprawdź czy pole już istnieje (po FieldName)
            var existingField = allExistingFields.GetValueOrDefault(fieldDto.FieldName);
            
            Guid fieldId;
            CostEstimateTemplateFieldDefinitionBase field;
            
            int order = 0;
            if (parentFieldId == null && columnLayoutOrderMap.TryGetValue(fieldDto.FieldName, out var layoutOrder))
            {
                order = layoutOrder;
            }
            else if (orderInParent.HasValue)
            {
                order = orderInParent.Value;
            }
            
            if (existingField != null)
            {
                // UPDATE - pole już istnieje, zaktualizuj jego właściwości
                fieldId = existingField.Id;
                field = existingField;
                
                // Aktualizuj właściwości
                field.FieldType = (FieldType)fieldDto.FieldType;
                field.Label = fieldDto.Label;
                field.IsSortable = fieldDto.IsSortable;
                field.IsFilterable = fieldDto.IsFilterable;
                field.IsVisible = fieldDto.IsVisible;
                field.ParentFieldId = parentFieldId;
                field.Order = order;
                
                // Dodaj do listy do aktualizacji (według rzeczywistego scope pola)
                if (field is CostEstimateTemplateItemCalculatedFieldDefinition calculatedField)
                {
                    calculatedField.SumInGroup = fieldDto.SumInGroup;
                    calculatedField.SumInTotal = fieldDto.SumInTotal;
                }
                
                // Dodaj do słownika według rzeczywistego FieldScope pola
                var realScope = field.FieldScope;
                if (!fieldsToUpdateByScope.ContainsKey(realScope))
                {
                    fieldsToUpdateByScope[realScope] = new List<CostEstimateTemplateFieldDefinitionBase>();
                }
                fieldsToUpdateByScope[realScope].Add(field);
            }
            else
            {
                // INSERT - nowe pole
                fieldId = Guid.NewGuid();
                
                field = fieldScope switch
                {
                    FieldScope.Group => new CostEstimateTemplateGroupFieldDefinition
                    {
                        Id = fieldId,
                        TemplateId = templateId,
                        FieldScope = fieldScope,
                        FieldType = (FieldType)fieldDto.FieldType,
                        FieldName = fieldDto.FieldName,
                        Label = fieldDto.Label,
                        IsSortable = fieldDto.IsSortable,
                        IsFilterable = fieldDto.IsFilterable,
                        IsVisible = fieldDto.IsVisible,
                        ParentFieldId = parentFieldId,
                        Order = order
                    },
                    
                    FieldScope.ItemSystem => new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = fieldId,
                        TemplateId = templateId,
                        FieldScope = fieldScope,
                        FieldType = (FieldType)fieldDto.FieldType,
                        FieldName = fieldDto.FieldName,
                        Label = fieldDto.Label,
                        IsSortable = fieldDto.IsSortable,
                        IsFilterable = fieldDto.IsFilterable,
                        IsVisible = fieldDto.IsVisible,
                        ParentFieldId = parentFieldId,
                        Order = order
                    },
                    
                    FieldScope.ItemCalculated => new CostEstimateTemplateItemCalculatedFieldDefinition
                    {
                        Id = fieldId,
                        TemplateId = templateId,
                        FieldScope = fieldScope,
                        FieldType = (FieldType)fieldDto.FieldType,
                        FieldName = fieldDto.FieldName,
                        Label = fieldDto.Label,
                        IsSortable = fieldDto.IsSortable,
                        IsFilterable = fieldDto.IsFilterable,
                        IsVisible = fieldDto.IsVisible,
                        ParentFieldId = parentFieldId,
                        Order = order,
                        SumInGroup = fieldDto.SumInGroup,
                        SumInTotal = fieldDto.SumInTotal
                    },
                    
                    FieldScope.ItemGeneric => new CostEstimateTemplateItemGenericFieldDefinition
                    {
                        Id = fieldId,
                        TemplateId = templateId,
                        FieldScope = fieldScope,
                        FieldType = (FieldType)fieldDto.FieldType,
                        FieldName = fieldDto.FieldName,
                        Label = fieldDto.Label,
                        IsSortable = fieldDto.IsSortable,
                        IsFilterable = fieldDto.IsFilterable,
                        IsVisible = fieldDto.IsVisible,
                        ParentFieldId = parentFieldId,
                        Order = order
                    },
                    
                    _ => throw new ValidationApiException($"Unsupported FieldScope: {fieldScope}")
                };
                
                // Dodaj do słownika według rzeczywistego FieldScope pola
                if (!fieldsToInsertByScope.ContainsKey(fieldScope))
                {
                    fieldsToInsertByScope[fieldScope] = new List<CostEstimateTemplateFieldDefinitionBase>();
                }
                fieldsToInsertByScope[fieldScope].Add(field);
            }
            
            fieldNameToIdMap[fieldDto.FieldName] = fieldId;
            
            // Rekurencyjnie przetwórz child fields
            if (fieldDto.ChildFields != null && fieldDto.ChildFields.Any())
            {
                for (int i = 0; i < fieldDto.ChildFields.Count; i++)
                {
                    var childDto = fieldDto.ChildFields[i];
                    
                    // Określ RZECZYWISTY FieldScope child field (nie scope rodzica!)
                    var childFieldScope = Business.Implementation.Helpers.CostEstimateFieldTypeHelper.DetermineFieldScopeFromFieldType(childDto.FieldType);
                    
                    if (!childFieldScope.HasValue)
                    {
                        throw new ValidationApiException($"Unknown FieldType: {childDto.FieldType}");
                    }
                    
                    // Przekaż RZECZYWISTY scope child field
                    CollectFieldsForUpsert(
                        childDto,
                        templateId,
                        childFieldScope.Value,  // ✅ Rzeczywisty scope child field
                        parentFieldId: fieldId,
                        orderInParent: i,
                        fieldsToInsertByScope,
                        fieldsToUpdateByScope,
                        allExistingFields,
                        fieldNameToIdMap,
                        columnLayoutOrderMap);
                }
            }
        }
        
        /// <summary>
        /// Przelicza wszystkie kosztorysy używające danego szablonu
        /// Wywoływane po aktualizacji struktury szablonu (usunięcie/zmiana pól)
        /// </summary>
        private async Task RecalculateAllCostEstimatesForTemplate(
            Guid templateId,
            CancellationToken cancellationToken)
        {
            // Pobierz wszystkie kosztorysy używające tego szablonu
            // Include wszystko co potrzebne do obliczeń
            var costEstimates = await costEstimateRepository.GetBySearch(
                ce => ce.TemplateId == templateId && !ce.IsDeleted,
                q => q.Include(ce => ce.Template)
                        .ThenInclude(t => t.CalculatedFieldDefinitions)
                      .Include(ce => ce.AllGroups)
                        .ThenInclude(g => g.Items)
                        .ThenInclude(i => i.FieldValues)
                        .ThenInclude(fv => fv.FieldDefinition));
            
            // Przeliczy każdy kosztorys
            foreach (var costEstimate in costEstimates)
            {
                calculationService.RecalculateCostEstimate(costEstimate);
            }
            
            // Zapisz zmiany
            if (costEstimates.Any())
            {
                await costEstimateRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
