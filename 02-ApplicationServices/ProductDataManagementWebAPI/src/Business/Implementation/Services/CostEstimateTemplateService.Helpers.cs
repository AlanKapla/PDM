using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Helper methods for CostEstimateTemplateService
/// Extracted from UpdateCostEstimateTemplateCommandHandler
/// </summary>
public sealed partial class CostEstimateTemplateService
{
    private async Task UpdateCurrenciesAsync(
        Guid templateId,
        List<CurrencyDto> currencies,
        CancellationToken cancellationToken)
    {
        var existingCurrencies = (await _currencyRepository
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
            await _currencyRepository.UpdateRange(toUpdate);
        if (toInsert.Any())
            await _currencyRepository.InsertRange(toInsert);

        await _currencyRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateUnitsAsync(
        Guid templateId,
        List<UnitDto> units,
        CancellationToken cancellationToken)
    {
        var existingUnits = (await _unitRepository
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
            await _unitRepository.UpdateRange(toUpdate);
        if (toInsert.Any())
            await _unitRepository.InsertRange(toInsert);

        await _unitRepository.SaveChangesAsync(cancellationToken);
    }

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

    private async Task DeleteRemovedFieldsAsync(
        Guid templateId,
        List<FieldDefinitionDto>? newGroupFields,
        List<FieldDefinitionDto>? newSystemFields,
        List<FieldDefinitionDto>? newCalculatedFields,
        List<FieldDefinitionDto>? newGenericFields,
        CancellationToken cancellationToken)
    {
        var newFieldNames = new HashSet<Guid>();

        CollectFieldNames(newGroupFields, newFieldNames);
        CollectFieldNames(newSystemFields, newFieldNames);
        CollectFieldNames(newCalculatedFields, newFieldNames);
        CollectFieldNames(newGenericFields, newFieldNames);

        await DeleteFieldsNotInSetAsync(_groupFieldRepository, templateId, newFieldNames, cancellationToken);
        await DeleteFieldsNotInSetAsync(_systemFieldRepository, templateId, newFieldNames, cancellationToken);
        await DeleteFieldsNotInSetAsync(_calculatedFieldRepository, templateId, newFieldNames, cancellationToken);
        await DeleteFieldsNotInSetAsync(_genericFieldRepository, templateId, newFieldNames, cancellationToken);
    }

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

    private async Task DeleteFieldsNotInSetAsync<T>(
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

        var childFieldsToDelete = fieldsToDelete
            .Where(f => f.ParentFieldId.HasValue)
            .ToList();

        if (childFieldsToDelete.Any())
        {
            await repository.DeleteRange(childFieldsToDelete);
            await repository.SaveChangesAsync(cancellationToken);
        }

        var parentFieldsToDelete = fieldsToDelete
            .Where(f => !f.ParentFieldId.HasValue)
            .ToList();

        if (parentFieldsToDelete.Any())
        {
            await repository.DeleteRange(parentFieldsToDelete);
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertFieldsInBatchAsync(
        List<FieldDefinitionDto> fieldDtos,
        Guid templateId,
        FieldScope fieldScope,
        Dictionary<Guid, Guid> fieldNameToIdMap,
        Dictionary<Guid, int> columnLayoutOrderMap,
        CancellationToken cancellationToken)
    {
        var allExistingFields = new Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase>();

        foreach (var scope in Enum.GetValues<FieldScope>())
        {
            var scopeFields = await GetExistingFieldsByScopeAsync(templateId, scope);
            foreach (var field in scopeFields)
            {
                allExistingFields[field.FieldName] = field;
            }
        }

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

        foreach (var kvp in fieldsToUpdateByScope)
        {
            if (kvp.Value.Any())
            {
                await UpdateFieldsByScopeAsync(kvp.Key, kvp.Value, cancellationToken);
            }
        }

        foreach (var kvp in fieldsToInsertByScope)
        {
            if (kvp.Value.Any())
            {
                await InsertFieldsByScopeAsync(kvp.Key, kvp.Value, cancellationToken);
            }
        }
    }

    private async Task<List<CostEstimateTemplateFieldDefinitionBase>> GetExistingFieldsByScopeAsync(
        Guid templateId,
        FieldScope fieldScope)
    {
        return fieldScope switch
        {
            FieldScope.Group => (await _groupFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
            FieldScope.ItemSystem => (await _systemFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
            FieldScope.ItemCalculated => (await _calculatedFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
            FieldScope.ItemGeneric => (await _genericFieldRepository.GetBySearch(f => f.TemplateId == templateId))
                .Cast<CostEstimateTemplateFieldDefinitionBase>().ToList(),
            _ => new List<CostEstimateTemplateFieldDefinitionBase>()
        };
    }

    private async Task UpdateFieldsByScopeAsync(
        FieldScope fieldScope,
        List<CostEstimateTemplateFieldDefinitionBase> fields,
        CancellationToken cancellationToken)
    {
        switch (fieldScope)
        {
            case FieldScope.Group:
                await _groupFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateGroupFieldDefinition>());
                break;
            case FieldScope.ItemSystem:
                await _systemFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemSystemFieldDefinition>());
                break;
            case FieldScope.ItemCalculated:
                await _calculatedFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemCalculatedFieldDefinition>());
                break;
            case FieldScope.ItemGeneric:
                await _genericFieldRepository.UpdateRange(fields.Cast<CostEstimateTemplateItemGenericFieldDefinition>());
                break;
        }

        await _groupFieldRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task InsertFieldsByScopeAsync(
        FieldScope fieldScope,
        List<CostEstimateTemplateFieldDefinitionBase> fields,
        CancellationToken cancellationToken)
    {
        switch (fieldScope)
        {
            case FieldScope.Group:
                await _groupFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateGroupFieldDefinition>());
                break;
            case FieldScope.ItemSystem:
                await _systemFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemSystemFieldDefinition>());
                break;
            case FieldScope.ItemCalculated:
                await _calculatedFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemCalculatedFieldDefinition>());
                break;
            case FieldScope.ItemGeneric:
                await _genericFieldRepository.InsertRange(fields.Cast<CostEstimateTemplateItemGenericFieldDefinition>());
                break;
        }

        await _groupFieldRepository.SaveChangesAsync(cancellationToken);
    }

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
            fieldId = existingField.Id;
            field = existingField;

            field.FieldType = (FieldType)fieldDto.FieldType;
            field.Label = fieldDto.Label;
            field.IsSortable = fieldDto.IsSortable;
            field.IsFilterable = fieldDto.IsFilterable;
            field.IsVisible = fieldDto.IsVisible;
            field.ParentFieldId = parentFieldId;
            field.Order = order;

            if (field is CostEstimateTemplateItemCalculatedFieldDefinition calculatedField)
            {
                calculatedField.SumInGroup = fieldDto.SumInGroup;
                calculatedField.SumInTotal = fieldDto.SumInTotal;
            }

            var realScope = field.FieldScope;
            if (!fieldsToUpdateByScope.ContainsKey(realScope))
            {
                fieldsToUpdateByScope[realScope] = new List<CostEstimateTemplateFieldDefinitionBase>();
            }
            fieldsToUpdateByScope[realScope].Add(field);
        }
        else
        {
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

            if (!fieldsToInsertByScope.ContainsKey(fieldScope))
            {
                fieldsToInsertByScope[fieldScope] = new List<CostEstimateTemplateFieldDefinitionBase>();
            }
            fieldsToInsertByScope[fieldScope].Add(field);
        }

        fieldNameToIdMap[fieldDto.FieldName] = fieldId;

        if (fieldDto.ChildFields != null && fieldDto.ChildFields.Any())
        {
            for (int i = 0; i < fieldDto.ChildFields.Count; i++)
            {
                var childDto = fieldDto.ChildFields[i];

                var childFieldScope = Helpers.CostEstimateFieldTypeHelper.DetermineFieldScopeFromFieldType(childDto.FieldType);

                if (!childFieldScope.HasValue)
                {
                    throw new ValidationApiException($"Unknown FieldType: {childDto.FieldType}");
                }

                CollectFieldsForUpsert(
                    childDto,
                    templateId,
                    childFieldScope.Value,
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

    private async Task RecalculateAllCostEstimatesForTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var costEstimates = await _costEstimateRepository.GetBySearch(
            ce => ce.TemplateId == templateId && !ce.IsDeleted,
            q => q.Include(ce => ce.Template)
                    .ThenInclude(t => t.CalculatedFieldDefinitions)
                  .Include(ce => ce.AllGroups)
                    .ThenInclude(g => g.Items)
                    .ThenInclude(i => i.FieldValues)
                    .ThenInclude(fv => fv.FieldDefinition));

        foreach (var costEstimate in costEstimates)
        {
            _calculationService.RecalculateCostEstimate(costEstimate);
        }

        if (costEstimates.Any())
        {
            await _costEstimateRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
