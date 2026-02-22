using Business.Implementation.Helpers;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class TemplateStructureService : ITemplateStructureService
    {
        private readonly IRepository<CostEstimateTemplateCurrency> currencyRepository;
        private readonly IRepository<CostEstimateTemplateUnit> unitRepository;
        private readonly IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository;
        private readonly IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository;

        public TemplateStructureService(
            IRepository<CostEstimateTemplateCurrency> currencyRepository,
            IRepository<CostEstimateTemplateUnit> unitRepository,
            IRepository<CostEstimateTemplateGroupFieldDefinition> groupFieldRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateTemplateItemCalculatedFieldDefinition> calculatedFieldRepository,
            IRepository<CostEstimateTemplateItemGenericFieldDefinition> genericFieldRepository)
        {
            this.currencyRepository = currencyRepository;
            this.unitRepository = unitRepository;
            this.groupFieldRepository = groupFieldRepository;
            this.systemFieldRepository = systemFieldRepository;
            this.calculatedFieldRepository = calculatedFieldRepository;
            this.genericFieldRepository = genericFieldRepository;
        }

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
                fieldTypeConfig,
                sumInGroup,
                sumInTotal,
                childFields
            );
        }
    }
}
