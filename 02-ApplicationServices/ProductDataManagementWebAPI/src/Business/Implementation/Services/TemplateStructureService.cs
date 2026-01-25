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

        public async Task<CostEstimateTemplateVersionStructureWeb> BuildTemplateVersionStructureAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion version,
            CancellationToken cancellationToken = default)
        {
            var (currencies, units, groupHeaderFields, systemFields, calculatedFields, genericFields, summaryConfig, uiConfig)
                = await BuildCommonStructureAsync(template, version, cancellationToken);

            return new CostEstimateTemplateVersionStructureWeb(
                version.Id,
                version.VersionNumber,
                version.VersionName,
                currencies.OrderBy(c => c.Order).ToList(),
                units.OrderBy(u => u.Order).ToList(),
                groupHeaderFields,
                systemFields,
                calculatedFields,
                genericFields,
                summaryConfig,
                uiConfig
            );
        }

        public async Task<CostEstimateTemplateStructureWeb> BuildCostEstimateTemplateStructureAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion version,
            CancellationToken cancellationToken = default)
        {
            var (currencies, units, groupHeaderFields, systemFields, calculatedFields, genericFields, summaryConfig, uiConfig)
                = await BuildCommonStructureAsync(template, version, cancellationToken);

            return new CostEstimateTemplateStructureWeb(
                version.CanAddGroups,
                version.CanBranchGroups,
                version.MaxGroupLevel,
                version.AutoNumberGroups,
                version.GroupNumberFormat,
                currencies.OrderBy(c => c.Order).ToList(),
                units.OrderBy(u => u.Order).ToList(),
                groupHeaderFields,
                systemFields,
                calculatedFields,
                genericFields,
                summaryConfig,
                uiConfig
            );
        }

        private async Task<(
            IEnumerable<CurrencyWeb> currencies,
            IEnumerable<UnitWeb> units,
            List<FieldDefinitionWeb> groupHeaderFields,
            List<FieldDefinitionWeb> systemFields,
            List<FieldDefinitionWeb> calculatedFields,
            List<FieldDefinitionWeb> genericFields,
            SummaryConfigurationWeb? summaryConfig,
            UiConfigurationWeb? uiConfig
        )> BuildCommonStructureAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion version,
            CancellationToken cancellationToken)
        {
            var currencies = await currencyRepository.SelectAsync(
                c => c.TemplateVersionId == version.Id,
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
                u => u.TemplateVersionId == version.Id,
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
                f => f.TemplateVersionId == version.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var groupHeaderFields = groupHeaderFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var systemFieldsList = await systemFieldRepository.GetBySearch(
                f => f.TemplateVersionId == version.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var systemFields = systemFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var calculatedFieldsList = await calculatedFieldRepository.GetBySearch(
                f => f.TemplateVersionId == version.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var calculatedFields = calculatedFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            var genericFieldsList = await genericFieldRepository.GetBySearch(
                f => f.TemplateVersionId == version.Id && f.ParentFieldId == null,
                q => q.Include(f => f.ChildFields)
            );

            var genericFields = genericFieldsList
                .OrderBy(f => f.Order)
                .Select(f => BuildFieldDefinitionWebRecursive(f))
                .ToList();

            // Build SummaryConfigurationWeb from field flags
            SummaryConfigurationWeb? summaryConfig = null;
            var groupSummaryFields = calculatedFieldsList
                .Where(f => f.SumInGroup)
                .OrderBy(f => f.Order)
                .Select(f => new SummaryFieldWeb(
                    f.Id,
                    f.FieldName,
                    (int)f.FieldType,
                    f.Label,
                    (int)f.FieldScope,
                    f.Order
                ))
                .ToList();

            var totalSummaryFields = calculatedFieldsList
                .Where(f => f.SumInTotal)
                .OrderBy(f => f.Order)
                .Select(f => new SummaryFieldWeb(
                    f.Id,
                    f.FieldName,
                    (int)f.FieldType,
                    f.Label,
                    (int)f.FieldScope,
                    f.Order
                ))
                .ToList();

            bool showGroupSummary = groupSummaryFields.Any();
            bool showTotalSummary = totalSummaryFields.Any();
            
            if (showGroupSummary || showTotalSummary)
            {
                summaryConfig = new SummaryConfigurationWeb(
                    showGroupSummary,
                    showTotalSummary,
                    groupSummaryFields,
                    totalSummaryFields
                );
            }

            // Build UiConfigurationWeb from field Order (all parent fields are visible by default)
            var allFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
            allFieldsList.AddRange(groupHeaderFieldsList);
            allFieldsList.AddRange(systemFieldsList);
            allFieldsList.AddRange(calculatedFieldsList);
            allFieldsList.AddRange(genericFieldsList);

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
                    IsVisible: true,
                    Width: null
                ))
                .ToList();

            UiConfigurationWeb? uiConfig = columns.Any() 
                ? new UiConfigurationWeb(columns) 
                : null;

            return (currencies, units, groupHeaderFields, systemFields, calculatedFields, genericFields, summaryConfig, uiConfig);
        }

        /// <summary>
        /// Rekurencyjnie buduje FieldDefinitionWeb z hierarchią child fields
        /// </summary>
        private FieldDefinitionWeb BuildFieldDefinitionWebRecursive(CostEstimateTemplateFieldDefinitionBase field)
        {
            List<FieldDefinitionWeb>? childFields = null;

            if (field.ChildFields != null && field.ChildFields.Any())
            {
                childFields = field.ChildFields
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

            return new FieldDefinitionWeb(
                field.Id,
                field.FieldName,
                field.Label,
                field.IsSortable,
                field.IsFilterable,
                fieldTypeConfig,
                childFields
            );
        }
    }
}
