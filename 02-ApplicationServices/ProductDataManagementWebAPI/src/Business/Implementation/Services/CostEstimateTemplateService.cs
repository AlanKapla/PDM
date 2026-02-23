using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class CostEstimateTemplateService : ICostEstimateTemplateService
    {
        private const string CacheKeyPrefix = "platform:template:";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateCurrency> currencyRepository;
        private readonly IRepository<CostEstimateTemplateUnit> unitRepository;
        private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository;
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly ICacheService cacheService;

        public CostEstimateTemplateService(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateCurrency> currencyRepository,
            IRepository<CostEstimateTemplateUnit> unitRepository,
            IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
            IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository,
            IRepository<CostEstimate> costEstimateRepository,
            ICostEstimateCalculationService calculationService,
            ICacheService cacheService)
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
            this.cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<Guid> CreateTemplateAsync(
            Guid ownerId,
            string name,
            string? description,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var template = new CostEstimateTemplate
            {
                OwnerId = ownerId,
                Name = name,
                Description = description,
                Category = null,
                CanAddGroups = true,
                CanBranchGroups = true,
                MaxGroupLevel = null,
                AutoNumberGroups = false,
                GroupNumberFormat = null,
                CreatedAt = now,
                IsDeleted = false
            };

            await templateRepository.Insert(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            return template.Id;
        }

        public async Task UpdateTemplateAsync(
            CostEstimateTemplate template,
            string name,
            string? description,
            string? category,
            bool canAddGroups,
            bool canBranchGroups,
            int? maxGroupLevel,
            bool autoNumberGroups,
            string? groupNumberFormat,
            bool updateStructure,
            List<CurrencyDto>? currencies,
            List<UnitDto>? units,
            List<FieldDefinitionDto>? groupHeaderFields,
            List<FieldDefinitionDto>? systemFields,
            List<FieldDefinitionDto>? calculatedFields,
            List<FieldDefinitionDto>? genericFields,
            UiConfigurationDto? uiConfiguration,
            CancellationToken cancellationToken = default)
        {
            template.Name = name;
            template.Description = description;
            template.Category = category;
            template.CanAddGroups = canAddGroups;
            template.CanBranchGroups = canBranchGroups;
            template.MaxGroupLevel = maxGroupLevel;
            template.AutoNumberGroups = autoNumberGroups;
            template.GroupNumberFormat = groupNumberFormat;
            template.UpdatedAt = DateTime.UtcNow;

            await templateRepository.Update(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            if (currencies != null)
            {
                await UpdateCurrenciesAsync(template.Id, currencies, cancellationToken);
            }

            if (units != null)
            {
                await UpdateUnitsAsync(template.Id, units, cancellationToken);
            }

            if (updateStructure)
            {
                await DeleteRemovedFieldsAsync(
                    template.Id,
                    groupHeaderFields,
                    systemFields,
                    calculatedFields,
                    genericFields,
                    cancellationToken);

                var fieldNameToIdMap = new Dictionary<Guid, Guid>();
                var columnLayoutOrderMap = BuildColumnLayoutOrderMap(uiConfiguration?.ColumnLayout);

                if (groupHeaderFields != null)
                {
                    await UpsertFieldsInBatchAsync(
                        groupHeaderFields, template.Id, FieldScope.Group,
                        fieldNameToIdMap, columnLayoutOrderMap, cancellationToken);
                }

                if (systemFields != null)
                {
                    await UpsertFieldsInBatchAsync(
                        systemFields, template.Id, FieldScope.ItemSystem,
                        fieldNameToIdMap, columnLayoutOrderMap, cancellationToken);
                }

                if (calculatedFields != null)
                {
                    await UpsertFieldsInBatchAsync(
                        calculatedFields, template.Id, FieldScope.ItemCalculated,
                        fieldNameToIdMap, columnLayoutOrderMap, cancellationToken);
                }

                if (genericFields != null)
                {
                    await UpsertFieldsInBatchAsync(
                        genericFields, template.Id, FieldScope.ItemGeneric,
                        fieldNameToIdMap, columnLayoutOrderMap, cancellationToken);
                }

                await RecalculateAllCostEstimatesForTemplateAsync(template.Id, cancellationToken);
            }

            await InvalidateTemplateCacheAsync(template.Id, cancellationToken);
        }

        public async Task DeleteTemplateAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default)
        {
            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;

            await templateRepository.Update(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            await InvalidateTemplateCacheAsync(template.Id, cancellationToken);
        }

        #endregion

        #region Cache Operations

        public async Task<CostEstimateTemplateStructureWeb> GetTemplateStructureCachedAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}{template.Id}";

            var cached = await cacheService.GetOrAddAsync(
                cacheKey,
                () => BuildTemplateStructureAsync(template, cancellationToken),
                CacheExpiration,
                cancellationToken);

            return cached!;
        }

        public async Task InvalidateTemplateCacheAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}{templateId}";
            await cacheService.RemoveCacheByKeyAsync(cacheKey, cancellationToken);
        }

        #endregion

        #region Structure Building

        public async Task<CostEstimateTemplateStructureWeb> BuildTemplateStructureAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default)
        {
            var currencies = await currencyRepository.SelectAsync(
                c => c.TemplateId == template.Id,
                c => new CurrencyWeb(
                    c.Id,
                    c.Code,
                    c.Name,
                    c.Symbol,
                    c.IsDefault,
                    c.Order
                ),
                cancellationToken
            );

            var units = await unitRepository.SelectAsync(
                u => u.TemplateId == template.Id,
                u => new UnitWeb(
                    u.Id,
                    u.Code,
                    u.Name,
                    u.Symbol,
                    u.Category,
                    u.IsDefault,
                    u.Order
                ),
                cancellationToken
            );

            var groupHeaderFieldsList = await groupFieldRepository.GetBySearch(
                f => f.TemplateId == template.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var groupHeaderFields = groupHeaderFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var systemFieldsList = await systemFieldRepository.GetBySearch(
                f => f.TemplateId == template.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var systemFields = systemFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var calculatedFieldsList = await calculatedFieldRepository.GetBySearch(
                f => f.TemplateId == template.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var calculatedFields = calculatedFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var genericFieldsList = await genericFieldRepository.GetBySearch(
                f => f.TemplateId == template.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var genericFields = genericFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var allFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
            allFieldsList.AddRange(groupHeaderFieldsList);
            allFieldsList.AddRange(systemFieldsList);
            allFieldsList.AddRange(calculatedFieldsList);
            allFieldsList.AddRange(genericFieldsList);

            var columns = allFieldsList
                .Where(f => f.ParentFieldId == null && f.IsVisible)
                .OrderBy(f => f.Order)
                .Select(f => new ColumnConfigurationWeb(
                    f.Id,
                    f.FieldName,
                    (int)f.FieldType,
                    f.Label,
                    (int)f.FieldScope,
                    f.Order
                ))
                .ToList();

            UiConfigurationWeb? uiConfig = columns.Any() 
                ? new UiConfigurationWeb(columns) 
                : null;

            return new CostEstimateTemplateStructureWeb(
                template.Id,
                template.MaxGroupLevel,
                currencies.OrderBy(c => c.Order).ToList(),
                units.OrderBy(u => u.Order).ToList(),
                groupHeaderFields,
                systemFields,
                calculatedFields,
                genericFields,
                uiConfig
            );
        }

        /// <summary>
        /// Rekurencyjnie buduje FieldDefinitionWeb z hierarchią child fields
        /// Child fields są sortowane według Order (zachowując kolejność z requestu)
        /// </summary>
        private FieldDefinitionWeb BuildFieldDefinitionWebRecursive(CostEstimateTemplateFieldDefinitionBase field)
        {
            List<FieldDefinitionWeb>? childFields = null;

            if (field.ChildFields != null && field.ChildFields.Any())
            {
                childFields = field.ChildFields
                    .OrderBy(cf => cf.Order)  // ✅ Sortuj według kolejności z requestu
                    .Select(cf => BuildFieldDefinitionWebRecursive(cf))
                    .ToList();
            }

            // Pobierz konfigurację typu pola
            var fieldTypeConfig = CostEstimateFieldTypeHelper.GetFieldTypeConfig(field.FieldType) ?? new CostEstimateFieldTypeConfigWeb(
                    FieldType: (int)field.FieldType,
                    FieldScope: (int)field.FieldScope,
                    NamePl: field.Label,
                    ValueTypeName: "string",
                    IsNumeric: false,
                    IsText: true,
                    IsDate: false,
                    IsBoolean: false,
                    IsCollection: false
                );

            // Pobierz SumInGroup i SumInTotal jeśli to pole typu ItemCalculated
            bool sumInGroup = false;
            bool sumInTotal = false;
            
            if (field is CostEstimateTemplateItemCalculatedFieldDefinition calculatedField)
            {
                sumInGroup = calculatedField.SumInGroup;
                sumInTotal = calculatedField.SumInTotal;
            }

            return new FieldDefinitionWeb(
                field.Id,
                field.FieldName,
                field.Label,
                field.IsSortable,
                field.IsFilterable,
                field.IsVisible,
                field.IsReadonly,
                fieldTypeConfig,
                sumInGroup,
                sumInTotal,
                childFields
            );
        }

        #endregion

        #region Currencies & Units

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

        #endregion

        #region Field Structure Management

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

            // Collect fields to delete per scope
            var groupToDelete = await CollectFieldsToDeleteAsync(groupFieldRepository, templateId, newFieldNames);
            var systemToDelete = await CollectFieldsToDeleteAsync(systemFieldRepository, templateId, newFieldNames);
            var calculatedToDelete = await CollectFieldsToDeleteAsync(calculatedFieldRepository, templateId, newFieldNames);
            var genericToDelete = await CollectFieldsToDeleteAsync(genericFieldRepository, templateId, newFieldNames);

            // First pass: delete child fields (with ParentFieldId) across ALL scopes
            // to avoid FK violations on the self-referencing ParentFieldId constraint (TPH single table)
            await DeleteFilteredFieldsAsync(groupFieldRepository, groupToDelete.Where(f => f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(systemFieldRepository, systemToDelete.Where(f => f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(calculatedFieldRepository, calculatedToDelete.Where(f => f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(genericFieldRepository, genericToDelete.Where(f => f.ParentFieldId.HasValue), cancellationToken);

            // Second pass: delete parent fields (without ParentFieldId)
            await DeleteFilteredFieldsAsync(groupFieldRepository, groupToDelete.Where(f => !f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(systemFieldRepository, systemToDelete.Where(f => !f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(calculatedFieldRepository, calculatedToDelete.Where(f => !f.ParentFieldId.HasValue), cancellationToken);
            await DeleteFilteredFieldsAsync(genericFieldRepository, genericToDelete.Where(f => !f.ParentFieldId.HasValue), cancellationToken);
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

        private async Task<List<T>> CollectFieldsToDeleteAsync<T>(
            IRepository<T> repository,
            Guid templateId,
            HashSet<Guid> keepFieldNames) where T : CostEstimateTemplateFieldDefinitionBase
        {
            var existingFields = await repository.GetBySearch(f => f.TemplateId == templateId);
            return existingFields.Where(f => !keepFieldNames.Contains(f.FieldName)).ToList();
        }

        private async Task DeleteFilteredFieldsAsync<T>(
            IRepository<T> repository,
            IEnumerable<T> fields,
            CancellationToken cancellationToken) where T : CostEstimateTemplateFieldDefinitionBase
        {
            var fieldsList = fields.ToList();
            if (!fieldsList.Any()) return;

            await repository.DeleteRange(fieldsList);
            await repository.SaveChangesAsync(cancellationToken);
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
                var scopeFields = await GetExistingFieldsByScope(templateId, scope);
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
                    await UpdateFieldsByScope(kvp.Key, kvp.Value, cancellationToken);
                }
            }

            foreach (var kvp in fieldsToInsertByScope)
            {
                if (kvp.Value.Any())
                {
                    await InsertFieldsByScope(kvp.Key, kvp.Value, cancellationToken);
                }
            }
        }

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
                field.IsReadonly = fieldDto.IsReadonly;
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
                        IsReadonly = fieldDto.IsReadonly,
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
                        IsReadonly = fieldDto.IsReadonly,
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
                        IsReadonly = fieldDto.IsReadonly,
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
                        IsReadonly = fieldDto.IsReadonly,
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

                    var childFieldScope = CostEstimateFieldTypeHelper.DetermineFieldScopeFromFieldType(childDto.FieldType);

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

        #endregion

        #region Cost Estimate Recalculation

        private async Task RecalculateAllCostEstimatesForTemplateAsync(
            Guid templateId,
            CancellationToken cancellationToken)
        {
            var costEstimates = await costEstimateRepository.GetBySearch(
                ce => ce.TemplateId == templateId && !ce.IsDeleted,
                q => q.Include(ce => ce.Template)
                        .ThenInclude(t => t.CalculatedFieldDefinitions)
                      .Include(ce => ce.AllGroups)
                        .ThenInclude(g => g.Items)
                        .ThenInclude(i => i.FieldValues)
                        .ThenInclude(fv => fv.FieldDefinition));

            foreach (var costEstimate in costEstimates)
            {
                calculationService.RecalculateCostEstimate(costEstimate);
            }

            if (costEstimates.Any())
            {
                await costEstimateRepository.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion
    }
}
