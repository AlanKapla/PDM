using System.Reflection;
using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class CostEstimateTemplateService : ICostEstimateTemplateService
    {
        private const string CacheKeyPrefix = "platform:template:";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateUnit> unitRepository;
        private readonly IRepository<CostEstimateTemplateCategory> categoryRepository;
        private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository;
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICacheService cacheService;
        private readonly ILogger<CostEstimateTemplateService> logger;

        public CostEstimateTemplateService(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateUnit> unitRepository,
            IRepository<CostEstimateTemplateCategory> categoryRepository,
            IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
            IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository,
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateFieldFile> fieldFileRepository,
            ICostEstimateCalculationService calculationService,
            IBlobStorageService blobStorageService,
            ICacheService cacheService,
            ILogger<CostEstimateTemplateService> logger)
        {
            this.templateRepository = templateRepository;
            this.unitRepository = unitRepository;
            this.categoryRepository = categoryRepository;
            this.groupFieldRepository = groupFieldRepository;
            this.systemFieldRepository = systemFieldRepository;
            this.calculatedFieldRepository = calculatedFieldRepository;
            this.genericFieldRepository = genericFieldRepository;
            this.costEstimateRepository = costEstimateRepository;
            this.fieldFileRepository = fieldFileRepository;
            this.calculationService = calculationService;
            this.blobStorageService = blobStorageService;
            this.cacheService = cacheService;
            this.logger = logger;
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
            List<UnitDto>? units,
            List<CategoryDto>? categories,
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

            if (units != null)
            {
                await UpdateUnitsAsync(template.Id, units, cancellationToken);
            }

            if (categories != null)
            {
                await UpdateCategoriesAsync(template.Id, categories, cancellationToken);
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

            CostEstimateTemplateStructureWeb? cached = await cacheService.GetOrAddAsync(
                cacheKey,
                () => BuildTemplateStructureAsync(template, cancellationToken),
                CacheExpiration,
                cancellationToken);

            if (cached is null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), template.Id.ToString());
            }

            return cached;
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

            var categories = await categoryRepository.SelectAsync(
                c => c.TemplateId == template.Id,
                c => new CategoryWeb(
                    c.Id,
                    c.Name,
                    c.Symbol,
                    c.Order
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

            // Kolumny budujemy bez filtrowania po IsVisible — cache jest neutralny.
            // Filtrowanie po IsVisible odbywa się na poziomie query, w zależności od access level:
            // Full access widzi wszystkie kolumny, Restricted widzi tylko IsVisible = true.
            var columns = allFieldsList
                .Where(f => f.ParentFieldId == null)
                .OrderBy(f => f.Order)
                .Select(f => new ColumnConfigurationWeb(
                    f.Id,
                    f.FieldName,
                    (int)f.FieldType,
                    f.Label,
                    (int)f.FieldScope,
                    f.Order,
                    f.IsVisible
                ))
                .ToList();

            UiConfigurationWeb? uiConfig = columns.Any() 
                ? new UiConfigurationWeb(columns) 
                : null;

            return new CostEstimateTemplateStructureWeb(
                template.Id,
                template.MaxGroupLevel,
                units.OrderBy(u => u.Order).ToList(),
                categories.OrderBy(c => c.Order).ToList(),
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

        #region Units & Categories

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

        private async Task UpdateCategoriesAsync(
            Guid templateId,
            List<CategoryDto> categories,
            CancellationToken cancellationToken)
        {
            var existingCategories = (await categoryRepository
                .GetBySearch(c => c.TemplateId == templateId)).ToList();

            var toUpdate = new List<CostEstimateTemplateCategory>();
            var toInsert = new List<CostEstimateTemplateCategory>();
            var incomingNames = categories.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toDelete = existingCategories
                .Where(c => !incomingNames.Contains(c.Name))
                .ToList();

            foreach (var categoryDto in categories)
            {
                var existing = existingCategories.FirstOrDefault(c =>
                    string.Equals(c.Name, categoryDto.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Symbol = categoryDto.Symbol;
                    existing.Order = categoryDto.Order;
                    toUpdate.Add(existing);
                }
                else
                {
                    toInsert.Add(new CostEstimateTemplateCategory
                    {
                        TemplateId = templateId,
                        Name = categoryDto.Name,
                        Symbol = categoryDto.Symbol,
                        Order = categoryDto.Order
                    });
                }
            }

            if (toDelete.Any())
                await categoryRepository.DeleteRange(toDelete);
            if (toUpdate.Any())
                await categoryRepository.UpdateRange(toUpdate);
            if (toInsert.Any())
                await categoryRepository.InsertRange(toInsert);

            await categoryRepository.SaveChangesAsync(cancellationToken);
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

            // Delete blob files for ItemSystemFiles fields being removed
            var fileFieldIds = systemToDelete
                .Where(f => f.FieldType == FieldType.ItemSystemFiles)
                .Select(f => f.Id)
                .ToHashSet();

            if (fileFieldIds.Count > 0)
            {
                await DeleteBlobFilesForFieldDefinitionsAsync(fileFieldIds, cancellationToken);
            }

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

        private async Task DeleteBlobFilesForFieldDefinitionsAsync(
            HashSet<Guid> fieldDefinitionIds,
            CancellationToken cancellationToken)
        {
            var filesToDelete = (await fieldFileRepository.GetBySearch(
                f => fieldDefinitionIds.Contains(f.FieldValue.FieldDefinitionId) &&
                     !f.IsDeleted)).ToList();

            if (filesToDelete.Count == 0)
            {
                return;
            }

            // Only delete blobs from Azure — DB records will be cascade-deleted
            // when the field definition is removed
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);

            foreach (var file in filesToDelete)
            {
                await blobStorageService.DeleteAsync(containerName, file.BlobName, cancellationToken);
            }

            logger.LogInformation(
                "Deleted {FileCount} blobs for removed field definitions {FieldDefinitionIds}",
                filesToDelete.Count, string.Join(", ", fieldDefinitionIds));
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
                      .Include(ce => ce.Template)
                        .ThenInclude(t => t.SystemFieldDefinitions)
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

        #region Default Templates

        private static readonly Lazy<Dictionary<string, DefaultTemplateJson>> DefaultTemplatesCache = new(LoadDefaultTemplatesFromResources);

        private static readonly JsonSerializerOptions DefaultTemplateJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public List<DefaultCostEstimateTemplateListItemWeb> GetDefaultTemplates()
        {
            return DefaultTemplatesCache.Value.Values
                .Select(t => new DefaultCostEstimateTemplateListItemWeb(
                    Slug: t.Slug,
                    Name: t.Name,
                    Description: t.Description,
                    Category: t.Category
                ))
                .ToList();
        }

        public CostEstimateTemplateStructureWeb? GetDefaultTemplateDetails(string slug)
        {
            if (!DefaultTemplatesCache.Value.TryGetValue(slug, out var template))
            {
                return null;
            }

            return MapDefaultTemplateToStructure(template);
        }

        private static Dictionary<string, DefaultTemplateJson> LoadDefaultTemplatesFromResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var result = new Dictionary<string, DefaultTemplateJson>(StringComparer.OrdinalIgnoreCase);

            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.Contains("DefaultTemplates") && n.EndsWith(".json"));

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                var template = JsonSerializer.Deserialize<DefaultTemplateJson>(stream, DefaultTemplateJsonOptions);
                if (template != null)
                {
                    result[template.Slug] = template;
                }
            }

            return result;
        }

        private static CostEstimateTemplateStructureWeb MapDefaultTemplateToStructure(DefaultTemplateJson template)
        {
            var units = template.Units
                .Select(u => new UnitWeb(
                    GenerateDeterministicGuid($"{template.Slug}:unit:{u.Code}"),
                    u.Code, u.Name, u.Symbol, u.Category, u.IsDefault, u.Order))
                .OrderBy(u => u.Order)
                .ToList();

            var groupHeaderFields = MapFieldDtosToWeb(template.GroupHeaderFields);
            var systemFields = MapFieldDtosToWeb(template.SystemFields);
            var calculatedFields = MapFieldDtosToWeb(template.CalculatedFields);
            var genericFields = MapFieldDtosToWeb(template.GenericFields);

            var allFields = template.GroupHeaderFields
                .Concat(template.SystemFields)
                .Concat(template.CalculatedFields)
                .Concat(template.GenericFields)
                .Where(f => f.IsVisible)
                .ToList();

            var columns = allFields
                .Select((f, index) => new ColumnConfigurationWeb(
                    FieldId: f.FieldName,
                    FieldName: f.FieldName,
                    FieldType: f.FieldType,
                    FieldLabel: f.Label,
                    FieldScope: CostEstimateFieldTypeHelper.GetFieldTypeConfig(f.FieldType)?.FieldScope ?? 0,
                    Order: index
                ))
                .ToList();

            var uiConfig = columns.Count > 0 ? new UiConfigurationWeb(columns) : null;

            return new CostEstimateTemplateStructureWeb(
                TemplateId: template.TemplateId,
                MaxGroupLevel: template.MaxGroupLevel,
                Units: units,
                Categories: template.Categories
                    .Select(c => new CategoryWeb(
                        GenerateDeterministicGuid($"{template.Slug}:category:{c.Name}"),
                        c.Name, c.Symbol, c.Order))
                    .OrderBy(c => c.Order)
                    .ToList(),
                GroupHeaderFields: groupHeaderFields,
                SystemFields: systemFields,
                CalculatedFields: calculatedFields,
                GenericFields: genericFields,
                UiConfiguration: uiConfig
            );
        }

        private static List<FieldDefinitionWeb> MapFieldDtosToWeb(List<FieldDefinitionDto> fields)
        {
            return fields.Select(MapFieldDtoToWeb).ToList();
        }

        private static FieldDefinitionWeb MapFieldDtoToWeb(FieldDefinitionDto field)
        {
            var fieldTypeConfig = CostEstimateFieldTypeHelper.GetFieldTypeConfig(field.FieldType)
                ?? new CostEstimateFieldTypeConfigWeb(
                    FieldType: field.FieldType,
                    FieldScope: 0,
                    NamePl: field.Label,
                    ValueTypeName: "string",
                    IsNumeric: false,
                    IsText: true,
                    IsDate: false,
                    IsBoolean: false,
                    IsCollection: false
                );

            List<FieldDefinitionWeb>? childFields = null;
            if (field.ChildFields != null && field.ChildFields.Count > 0)
            {
                childFields = MapFieldDtosToWeb(field.ChildFields);
            }

            return new FieldDefinitionWeb(
                Id: field.FieldName,
                FieldName: field.FieldName,
                Label: field.Label,
                IsSortable: field.IsSortable,
                IsFilterable: field.IsFilterable,
                IsVisible: field.IsVisible,
                IsReadonly: field.IsReadonly,
                FieldTypeConfig: fieldTypeConfig,
                SumInGroup: field.SumInGroup,
                SumInTotal: field.SumInTotal,
                ChildFields: childFields
            );
        }

        private static Guid GenerateDeterministicGuid(string seed)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
            return new Guid(hash.AsSpan(0, 16));
        }

        public async Task<Guid> CreateTemplateFromDefaultAsync(
            Guid ownerId,
            string slug,
            string name,
            string? description,
            CancellationToken cancellationToken = default)
        {
            if (!DefaultTemplatesCache.Value.TryGetValue(slug, out var defaultTemplate))
            {
                throw new NotFoundApiException("Default template", slug);
            }

            var templateId = await CreateTemplateAsync(ownerId, name, description, cancellationToken);

            var template = await templateRepository.GetFirstBySearch(t => t.Id == templateId)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), templateId.ToString());

            var groupFields = RegenerateFieldGuids(defaultTemplate.GroupHeaderFields);
            var systemFields = RegenerateFieldGuids(defaultTemplate.SystemFields);
            var calculatedFields = RegenerateFieldGuids(defaultTemplate.CalculatedFields);
            var genericFields = RegenerateFieldGuids(defaultTemplate.GenericFields);

            var columnLayout = groupFields
                .Concat(systemFields)
                .Concat(calculatedFields)
                .Concat(genericFields)
                .Where(f => f.IsVisible)
                .Select(f => f.FieldName)
                .ToList();

            await UpdateTemplateAsync(
                template,
                name,
                description,
                defaultTemplate.Category,
                canAddGroups: true,
                canBranchGroups: true,
                maxGroupLevel: defaultTemplate.MaxGroupLevel,
                autoNumberGroups: false,
                groupNumberFormat: null,
                updateStructure: true,
                defaultTemplate.Units,
                defaultTemplate.Categories,
                groupFields,
                systemFields,
                calculatedFields,
                genericFields,
                new UiConfigurationDto(columnLayout),
                cancellationToken);

            return templateId;
        }

        private static List<FieldDefinitionDto> RegenerateFieldGuids(List<FieldDefinitionDto> fields)
        {
            return fields.Select(f => f with
            {
                FieldName = Guid.NewGuid(),
                ChildFields = f.ChildFields != null ? RegenerateFieldGuids(f.ChildFields) : null
            }).ToList();
        }

        private static List<FieldDefinitionDto> MapFieldWebsToDtos(List<FieldDefinitionWeb> fields)
        {
            return fields.Select(f => new FieldDefinitionDto(
                FieldName: Guid.NewGuid(),
                FieldType: f.FieldTypeConfig.FieldType,
                Label: f.Label,
                IsSortable: f.IsSortable,
                IsFilterable: f.IsFilterable,
                IsVisible: f.IsVisible,
                IsReadonly: f.IsReadonly,
                SumInGroup: f.SumInGroup,
                SumInTotal: f.SumInTotal,
                ChildFields: f.ChildFields != null ? MapFieldWebsToDtos(f.ChildFields) : null
            )).ToList();
        }

        public async Task<Guid> DuplicateTemplateAsync(
            CostEstimateTemplate sourceTemplate,
            Guid ownerId,
            string name,
            string? description,
            CancellationToken cancellationToken = default)
        {
            var structure = await BuildTemplateStructureAsync(sourceTemplate, cancellationToken);

            var templateId = await CreateTemplateAsync(ownerId, name, description, cancellationToken);

            var newTemplate = await templateRepository.GetFirstBySearch(t => t.Id == templateId)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), templateId.ToString());

            var units = structure.Units
                .Select(u => new UnitDto(u.Code, u.Name, u.Symbol, u.Category, u.IsDefault, u.Order))
                .ToList();

            var categories = structure.Categories
                .Select(c => new CategoryDto(c.Name, c.Symbol, c.Order))
                .ToList();

            var groupFields = MapFieldWebsToDtos(structure.GroupHeaderFields);
            var systemFields = MapFieldWebsToDtos(structure.SystemFields);
            var calculatedFields = MapFieldWebsToDtos(structure.CalculatedFields);
            var genericFields = MapFieldWebsToDtos(structure.GenericFields);

            var columnLayout = groupFields
                .Concat(systemFields)
                .Concat(calculatedFields)
                .Concat(genericFields)
                .Where(f => f.IsVisible)
                .Select(f => f.FieldName)
                .ToList();

            await UpdateTemplateAsync(
                newTemplate,
                name,
                description,
                sourceTemplate.Category,
                sourceTemplate.CanAddGroups,
                sourceTemplate.CanBranchGroups,
                sourceTemplate.MaxGroupLevel,
                sourceTemplate.AutoNumberGroups,
                sourceTemplate.GroupNumberFormat,
                updateStructure: true,
                units,
                categories,
                groupFields,
                systemFields,
                calculatedFields,
                genericFields,
                new UiConfigurationDto(columnLayout),
                cancellationToken);

            return templateId;
        }

        #endregion
    }
}
