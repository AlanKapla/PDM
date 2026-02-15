using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;

namespace Business.Implementation.Services;

/// <summary>
/// Helper methods for CostEstimateService
/// Extracted from UpdateCostEstimateCommandHandler
/// </summary>
public sealed partial class CostEstimateService
{
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

            if (groupDto.Id.HasValue && existingGroups.Any(g => g.Id == groupDto.Id.Value))
            {
                // Update existing group
                group = existingGroups.First(g => g.Id == groupDto.Id.Value);
                groupId = group.Id;

                group.ParentGroupId = parentGroupId;
                group.Level = level;
                group.Order = groupDto.Order;
                group.UpdatedAt = now;

                await _groupRepository.Update(group);

                // Delete old field values
                var existingFieldValues = await _groupFieldValueRepository.GetBySearch(
                    fv => fv.GroupId == groupId);

                foreach (var fv in existingFieldValues)
                {
                    await _groupFieldValueRepository.Delete(fv);
                }
            }
            else
            {
                // Create new group
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

                await _groupRepository.Insert(group);
                await _groupRepository.SaveChangesAsync(cancellationToken);
            }

            // Create field values with typed properties
            var fieldValues = groupDto.FieldValues.Select(fv =>
            {
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

            var fieldValidation = _groupValidator.ValidateGroupFieldValues(
                groupFieldDefinitionsById,
                fieldValues,
                cancellationToken);

            if (!fieldValidation.IsValid)
            {
                throw new ValidationApiException($"Group field validation failed: {string.Join("; ", fieldValidation.Errors)}");
            }

            foreach (var fieldValue in fieldValues)
            {
                await _groupFieldValueRepository.Insert(fieldValue);
            }

            if (fieldValues.Any())
            {
                await _groupFieldValueRepository.SaveChangesAsync(cancellationToken);
            }

            // Update items
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
            await _itemRepository.Update(item);
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
        ItemRelationType relationType,
        CostEstimateItemDto itemDto,
        List<CostEstimateItem> existingItems,
        DateTime now,
        CancellationToken cancellationToken)
    {
        CostEstimateItem item;
        Guid itemId;

        if (itemDto.Id.HasValue && existingItems.Any(i => i.Id == itemDto.Id.Value))
        {
            item = existingItems.First(i => i.Id == itemDto.Id.Value);
            itemId = item.Id;

            item.ParentItemId = parentItemId;
            item.RelationType = relationType;
            item.Order = itemDto.Order;
            item.UpdatedAt = now;

            await _itemRepository.Update(item);

            var existingFieldValues = await _itemFieldValueRepository.GetBySearch(
                fv => fv.ItemId == itemId);

            foreach (var fv in existingFieldValues)
            {
                await _itemFieldValueRepository.Delete(fv);
            }
        }
        else
        {
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

            await _itemRepository.Insert(item);
            await _itemRepository.SaveChangesAsync(cancellationToken);
        }

        var itemFieldValues = itemDto.FieldValues.Select(fv =>
        {
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

        var itemFieldValidation = _itemValidator.ValidateItemFieldValues(
            allItemFieldDefinitionsById,
            itemFieldValues,
            cancellationToken);

        if (!itemFieldValidation.IsValid)
        {
            throw new ValidationApiException($"Item field validation failed: {string.Join("; ", itemFieldValidation.Errors)}");
        }

        foreach (var fieldValue in itemFieldValues)
        {
            await _itemFieldValueRepository.Insert(fieldValue);
        }

        if (itemFieldValues.Any())
        {
            await _itemFieldValueRepository.SaveChangesAsync(cancellationToken);
        }

        // Handle options
        if (itemDto.Options != null && itemDto.Options.Any())
        {
            if (relationType == ItemRelationType.Option)
            {
                throw new ValidationApiException(
                    $"Item {itemId}: Options cannot have their own Options. Maximum nesting: Position → Component → Option.");
            }

            ValidateOnlyOneOptionIsSelected(itemDto.Options, allItemFieldDefinitionsById);

            var existingOptions = await _itemRepository.GetBySearch(
                i => i.ParentItemId == itemId && !i.IsDeleted,
                q => q.Include(i => i.FieldValues));

            var existingOptionsList = existingOptions.ToList();

            foreach (var optionDto in itemDto.Options)
            {
                await UpdateSingleItemAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    parentItemId: itemId,
                    relationType: ItemRelationType.Option,
                    optionDto,
                    existingOptionsList,
                    now,
                    cancellationToken);
            }

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
                await _itemRepository.Update(option);
            }

            if (optionsToDelete.Any())
            {
                await _itemRepository.SaveChangesAsync(cancellationToken);
            }
        }

        // Handle components
        if (itemDto.Components != null && itemDto.Components.Any())
        {
            var calculatedFields = itemDto.FieldValues
                .Where(fv =>
                {
                    if (!allItemFieldDefinitionsById.TryGetValue(fv.FieldDefinitionId, out var fieldDef))
                    {
                        throw new ValidationApiException($"Field definition {fv.FieldDefinitionId} not found in template.");
                    }

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

            if (relationType != ItemRelationType.None)
            {
                throw new ValidationApiException(
                    $"Item {itemId}: Only main positions (RelationType=None) can have Components. " +
                    $"Components and Options cannot have their own Components.");
            }

            var existingComponents = await _itemRepository.GetBySearch(
                i => i.ParentItemId == itemId && i.RelationType == ItemRelationType.Component && !i.IsDeleted,
                q => q.Include(i => i.FieldValues));

            var existingComponentsList = existingComponents.ToList();

            foreach (var componentDto in itemDto.Components)
            {
                await UpdateSingleItemAsync(
                    costEstimateId,
                    allItemFieldDefinitionsById,
                    groupId,
                    parentItemId: itemId,
                    relationType: ItemRelationType.Component,
                    componentDto,
                    existingComponentsList,
                    now,
                    cancellationToken);
            }

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
                await _itemRepository.Update(component);
            }

            if (componentsToDelete.Any())
            {
                await _itemRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private void ValidateOnlyOneOptionIsSelected(
        List<CostEstimateItemDto> options,
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allItemFieldDefinitionsById)
    {
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
