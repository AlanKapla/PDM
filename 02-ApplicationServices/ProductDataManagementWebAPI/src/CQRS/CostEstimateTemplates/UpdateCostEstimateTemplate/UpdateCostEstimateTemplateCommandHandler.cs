using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.CostEstimateTemplates.Shared;
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
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly IRepository<CostEstimateTemplateCurrency> currencyRepository;
        private readonly IRepository<CostEstimateTemplateUnit> unitRepository;
        private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            IRepository<CostEstimateTemplateCurrency> currencyRepository,
            IRepository<CostEstimateTemplateUnit> unitRepository,
            IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
            IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.currencyRepository = currencyRepository;
            this.unitRepository = unitRepository;
            this.groupFieldRepository = groupFieldRepository;
            this.systemFieldRepository = systemFieldRepository;
            this.calculatedFieldRepository = calculatedFieldRepository;
            this.genericFieldRepository = genericFieldRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.Versions.Where(v => !v.IsDeleted)));
            
            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            var currentVersion = template.Versions.FirstOrDefault(v => v.Id == request.CurrentVersionId && !v.IsDeleted);
            if (currentVersion == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplateVersion), request.CurrentVersionId.ToString());
            }

            template.Name = request.Name;
            template.Description = request.Description;
            template.UpdatedAt = now;

            await templateRepository.Update(template);

            currentVersion.Category = request.Category;
            currentVersion.CanAddGroups = request.CanAddGroups;
            currentVersion.CanBranchGroups = request.CanBranchGroups;
            currentVersion.MaxGroupLevel = request.MaxGroupLevel;
            currentVersion.AutoNumberGroups = request.AutoNumberGroups;
            currentVersion.GroupNumberFormat = request.GroupNumberFormat;

            await versionRepository.Update(currentVersion);
            
            await templateRepository.SaveChangesAsync(cancellationToken);

            if (request.Currencies != null)
            {
                await UpdateCurrenciesAsync(currentVersion.Id, request.Currencies, cancellationToken);
            }

            if (request.Units != null)
            {
                await UpdateUnitsAsync(currentVersion.Id, request.Units, cancellationToken);
            }

            if (!request.UpdateStructure)
            {
                return Unit.Value;
            }

            CostEstimateTemplateVersion targetVersion = await GetOrCreateTargetVersionAsync(
                template, 
                currentVersion, 
                request, 
                now, 
                cancellationToken);

            await DeleteExistingStructureAsync(targetVersion.Id, cancellationToken);

            var fieldNameToIdMap = new Dictionary<Guid, Guid>();

            if (request.GroupHeaderFields != null)
            {
                await CreateFieldsInBatchAsync(
                    request.GroupHeaderFields,
                    targetVersion.Id,
                    FieldScope.Group,
                    fieldNameToIdMap,
                    request.SummaryConfiguration,
                    request.UiConfiguration,
                    cancellationToken);
            }

            if (request.SystemFields != null)
            {
                await CreateFieldsInBatchAsync(
                    request.SystemFields,
                    targetVersion.Id,
                    FieldScope.ItemSystem,
                    fieldNameToIdMap,
                    request.SummaryConfiguration,
                    request.UiConfiguration,
                    cancellationToken);
            }

            if (request.CalculatedFields != null)
            {
                await CreateFieldsInBatchAsync(
                    request.CalculatedFields,
                    targetVersion.Id,
                    FieldScope.ItemCalculated,
                    fieldNameToIdMap,
                    request.SummaryConfiguration,
                    request.UiConfiguration,
                    cancellationToken);
            }

            if (request.GenericFields != null)
            {
                await CreateFieldsInBatchAsync(
                    request.GenericFields,
                    targetVersion.Id,
                    FieldScope.ItemGeneric,
                    fieldNameToIdMap,
                    request.SummaryConfiguration,
                    request.UiConfiguration,
                    cancellationToken);
            }

            return Unit.Value;
        }
        
        /// <summary>
        /// Aktualizuje waluty dla wersji (update/insert)
        /// </summary>
        private async Task UpdateCurrenciesAsync(
            Guid versionId, 
            List<CurrencyDto> currencies, 
            CancellationToken cancellationToken)
        {
            var existingCurrencies = (await currencyRepository
                .GetBySearch(c => c.TemplateVersionId == versionId)).ToList();
            
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
                        TemplateVersionId = versionId,
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
        /// Aktualizuje jednostki dla wersji (update/insert)
        /// </summary>
        private async Task UpdateUnitsAsync(
            Guid versionId, 
            List<UnitDto> units, 
            CancellationToken cancellationToken)
        {
            var existingUnits = (await unitRepository
                .GetBySearch(u => u.TemplateVersionId == versionId)).ToList();
            
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
                        TemplateVersionId = versionId,
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
        /// Pobiera lub tworzy target version (Draft jeśli Approved, lub current)
        /// </summary>
        private async Task<CostEstimateTemplateVersion> GetOrCreateTargetVersionAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion currentVersion,
            UpdateCostEstimateTemplateCommand request,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (currentVersion.Status == TemplateVersionStatus.Approved)
            {
                var maxVersionNumber = template.Versions.Max(v => v.VersionNumber);
                
                var targetVersion = new CostEstimateTemplateVersion
                {
                    Id = Guid.NewGuid(),
                    TemplateId = template.Id,
                    VersionNumber = maxVersionNumber + 1,
                    VersionName = $"Draft v{maxVersionNumber + 1} (edited from Approved v{currentVersion.VersionNumber})",
                    Status = TemplateVersionStatus.Draft,
                    Category = request.Category,
                    CanAddGroups = request.CanAddGroups,
                    CanBranchGroups = request.CanBranchGroups,
                    MaxGroupLevel = request.MaxGroupLevel,
                    AutoNumberGroups = request.AutoNumberGroups,
                    GroupNumberFormat = request.GroupNumberFormat,
                    CreatedAt = now,
                    IsDeleted = false
                };

                await versionRepository.Insert(targetVersion);
                await versionRepository.SaveChangesAsync(cancellationToken);
                
                return targetVersion;
            }
            else
            {
                currentVersion.VersionName = request.Name;
                await versionRepository.Update(currentVersion);
                await versionRepository.SaveChangesAsync(cancellationToken);
                
                return currentVersion;
            }
        }
        
        /// <summary>
        /// Usuwa całą istniejącą strukturę dla wersji (tylko pola - summary i UI są teraz na polach)
        /// </summary>
        private async Task DeleteExistingStructureAsync(
            Guid versionId, 
            CancellationToken cancellationToken)
        {
            var existingGroupFields = await groupFieldRepository
                .GetBySearch(f => f.TemplateVersionId == versionId);
            
            if (existingGroupFields.Any())
            {
                await groupFieldRepository.DeleteRange(existingGroupFields);
            }
            
            var existingSystemFields = await systemFieldRepository
                .GetBySearch(f => f.TemplateVersionId == versionId);
            
            if (existingSystemFields.Any())
            {
                await systemFieldRepository.DeleteRange(existingSystemFields);
            }
            
            var existingCalculatedFields = await calculatedFieldRepository
                .GetBySearch(f => f.TemplateVersionId == versionId);
            
            if (existingCalculatedFields.Any())
            {
                await calculatedFieldRepository.DeleteRange(existingCalculatedFields);
            }
            
            var existingGenericFields = await genericFieldRepository
                .GetBySearch(f => f.TemplateVersionId == versionId);
            
            if (existingGenericFields.Any())
            {
                await genericFieldRepository.DeleteRange(existingGenericFields);
            }

            await groupFieldRepository.SaveChangesAsync(cancellationToken);
        }
        
        /// <summary>
        /// Tworzy pola w batch (z hierarchią) i ustawia Order, IsVisible, Width, SumInGroup, SumInTotal
        /// </summary>
        private async Task CreateFieldsInBatchAsync(
            List<FieldDefinitionDto> fieldDtos,
            Guid versionId,
            FieldScope fieldScope,
            Dictionary<Guid, Guid> fieldNameToIdMap,
            SummaryConfigurationDto? summaryConfig,
            UiConfigurationDto? uiConfig,
            CancellationToken cancellationToken)
        {
            var fieldsToInsert = new List<CostEstimateTemplateFieldDefinitionBase>();
            
            foreach (var fieldDto in fieldDtos)
            {
                CollectFieldsRecursive(
                    fieldDto,
                    versionId,
                    fieldScope,
                    parentFieldId: null,
                    fieldsToInsert,
                    fieldNameToIdMap,
                    summaryConfig,
                    uiConfig);
            }
            
            if (!fieldsToInsert.Any())
            {
                return;
            }
            
            var groupFields = fieldsToInsert.OfType<CostEstimateTemplateGroupFieldDefinition>().ToList();
            if (groupFields.Any())
            {
                await groupFieldRepository.InsertRange(groupFields);
            }
            
            var systemFields = fieldsToInsert.OfType<CostEstimateTemplateItemSystemFieldDefinition>().ToList();
            if (systemFields.Any())
            {
                await systemFieldRepository.InsertRange(systemFields);
            }
            
            var calculatedFields = fieldsToInsert.OfType<CostEstimateTemplateItemCalculatedFieldDefinition>().ToList();
            if (calculatedFields.Any())
            {
                await calculatedFieldRepository.InsertRange(calculatedFields);
            }
            
            var genericFields = fieldsToInsert.OfType<CostEstimateTemplateItemGenericFieldDefinition>().ToList();
            if (genericFields.Any())
            {
                await genericFieldRepository.InsertRange(genericFields);
            }
            
            if (fieldsToInsert.Any())
            {
                await groupFieldRepository.SaveChangesAsync(cancellationToken);
            }
        }
        
        /// <summary>
        /// Rekurencyjnie zbiera pola do insertu, buduje mapę FieldName -> Id
        /// i ustawia Order, SumInGroup, SumInTotal na podstawie konfiguracji
        /// </summary>
        private void CollectFieldsRecursive(
            FieldDefinitionDto fieldDto,
            Guid versionId,
            FieldScope fieldScope,
            Guid? parentFieldId,
            List<CostEstimateTemplateFieldDefinitionBase> fieldsToInsert,
            Dictionary<Guid, Guid> fieldNameToIdMap,
            SummaryConfigurationDto? summaryConfig,
            UiConfigurationDto? uiConfig)
        {
            var fieldId = Guid.NewGuid();
            
            fieldNameToIdMap[fieldDto.FieldName] = fieldId;
            
            // Determine Order from UiConfiguration (for parent fields only)
            int order = 0;
            
            if (parentFieldId == null && uiConfig != null)
            {
                var columnIndex = uiConfig.ColumnLayout?.IndexOf(fieldDto.FieldName) ?? -1;
                if (columnIndex >= 0)
                {
                    order = columnIndex;
                }
            }
            
            // Determine SumInGroup and SumInTotal from SummaryConfiguration (only for calculated fields)
            bool sumInGroup = false;
            bool sumInTotal = false;
            
            if (fieldScope == FieldScope.ItemCalculated && summaryConfig != null)
            {
                sumInGroup = summaryConfig.GroupSummaryFields?.Contains(fieldDto.FieldName) ?? false;
                sumInTotal = summaryConfig.TotalSummaryFields?.Contains(fieldDto.FieldName) ?? false;
            }
            
            CostEstimateTemplateFieldDefinitionBase field = fieldScope switch
            {
                FieldScope.Group => new CostEstimateTemplateGroupFieldDefinition
                {
                    Id = fieldId,
                    TemplateVersionId = versionId,
                    FieldScope = fieldScope,
                    FieldType = (FieldType)fieldDto.FieldType,
                    FieldName = fieldDto.FieldName,
                    Label = fieldDto.Label,
                    IsSortable = fieldDto.IsSortable,
                    IsFilterable = fieldDto.IsFilterable,
                    ParentFieldId = parentFieldId,
                    Order = order
                },
                
                FieldScope.ItemSystem => new CostEstimateTemplateItemSystemFieldDefinition
                {
                    Id = fieldId,
                    TemplateVersionId = versionId,
                    FieldScope = fieldScope,
                    FieldType = (FieldType)fieldDto.FieldType,
                    FieldName = fieldDto.FieldName,
                    Label = fieldDto.Label,
                    IsSortable = fieldDto.IsSortable,
                    IsFilterable = fieldDto.IsFilterable,
                    ParentFieldId = parentFieldId,
                    Order = order
                },
                
                FieldScope.ItemCalculated => new CostEstimateTemplateItemCalculatedFieldDefinition
                {
                    Id = fieldId,
                    TemplateVersionId = versionId,
                    FieldScope = fieldScope,
                    FieldType = (FieldType)fieldDto.FieldType,
                    FieldName = fieldDto.FieldName,
                    Label = fieldDto.Label,
                    IsSortable = fieldDto.IsSortable,
                    IsFilterable = fieldDto.IsFilterable,
                    ParentFieldId = parentFieldId,
                    Order = order,
                    SumInGroup = sumInGroup,
                    SumInTotal = sumInTotal
                },
                
                FieldScope.ItemGeneric => new CostEstimateTemplateItemGenericFieldDefinition
                {
                    Id = fieldId,
                    TemplateVersionId = versionId,
                    FieldScope = fieldScope,
                    FieldType = (FieldType)fieldDto.FieldType,
                    FieldName = fieldDto.FieldName,
                    Label = fieldDto.Label,
                    IsSortable = fieldDto.IsSortable,
                    IsFilterable = fieldDto.IsFilterable,
                    ParentFieldId = parentFieldId,
                    Order = order
                },
                
                _ => throw new ValidationApiException($"Unsupported FieldScope: {fieldScope}")
            };
            
            fieldsToInsert.Add(field);
            
            if (fieldDto.ChildFields != null && fieldDto.ChildFields.Any())
            {
                foreach (var childDto in fieldDto.ChildFields)
                {
                    var childFieldScope = Business.Implementation.Helpers.CostEstimateFieldTypeHelper.DetermineFieldScopeFromFieldType(childDto.FieldType);
                    
                    if (!childFieldScope.HasValue)
                    {
                        throw new ValidationApiException($"Unknown FieldType: {childDto.FieldType}");
                    }
                    
                    CollectFieldsRecursive(
                        childDto,
                        versionId,
                        childFieldScope.Value,
                        parentFieldId: fieldId,
                        fieldsToInsert,
                        fieldNameToIdMap,
                        summaryConfig,
                        uiConfig);
                }
            }
        }
    }
}
