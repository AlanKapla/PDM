using Business.Implementation.Helpers;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class CostEstimateService : ICostEstimateService
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository;
        private readonly IRepository<CostEstimateItemFieldValue> fieldValueRepository;
        private readonly ILogger<CostEstimateService> logger;

        public CostEstimateService(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> systemFieldRepository,
            IRepository<CostEstimateItemFieldValue> fieldValueRepository,
            ILogger<CostEstimateService> logger)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.systemFieldRepository = systemFieldRepository;
            this.fieldValueRepository = fieldValueRepository;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task AddSelectedFieldToExistingItemsAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            var selectedFieldDefinition = await systemFieldRepository.GetFirstBySearch(
                f => f.TemplateId == templateId
                     && f.FieldType == FieldType.ItemSystemSelected
                     && !f.ParentFieldId.HasValue);

            if (selectedFieldDefinition == null)
            {
                return;
            }

            var costEstimates = await costEstimateRepository.GetBySearch(
                ce => ce.TemplateId == templateId && !ce.IsDeleted,
                q => q.Include(ce => ce.AllGroups)
                      .ThenInclude(g => g.Items)
                      .ThenInclude(i => i.FieldValues));

            var now = DateTime.UtcNow;
            var fieldValuesToInsert = new List<CostEstimateItemFieldValue>();

            foreach (var costEstimate in costEstimates)
            {
                var allItems = costEstimate.AllGroups
                    .Where(g => !g.IsDeleted)
                    .SelectMany(g => g.Items)
                    .Where(i => !i.IsDeleted && !i.ParentItemId.HasValue);

                foreach (var item in allItems)
                {
                    bool alreadyHasSelectedField = item.FieldValues
                        .Any(fv => fv.FieldDefinitionId == selectedFieldDefinition.Id);

                    if (!alreadyHasSelectedField)
                    {
                        fieldValuesToInsert.Add(new CostEstimateItemFieldValue
                        {
                            ItemId = item.Id,
                            FieldDefinitionId = selectedFieldDefinition.Id,
                            BoolValue = true,
                            CreatedAt = now
                        });
                    }
                }
            }

            if (fieldValuesToInsert.Count > 0)
            {
                await fieldValueRepository.InsertRange(fieldValuesToInsert);
                await fieldValueRepository.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Added ItemSystemSelected field with value true to {Count} items for template {TemplateId}",
                    fieldValuesToInsert.Count,
                    templateId);
            }
        }

        /// <inheritdoc />
        public List<CostEstimateItemFieldValue> CreateDefaultItemFieldValues(
            Guid itemId,
            CostEstimateTemplate template,
            ItemRelationType relationType,
            bool hasParent,
            DateTime now)
        {
            var fieldValues = new List<CostEstimateItemFieldValue>();

            // Collect all item field definitions from the template
            var allFieldDefinitions = template.SystemFieldDefinitions
                .Cast<CostEstimateTemplateFieldDefinitionBase>()
                .Concat(template.CalculatedFieldDefinitions)
                .Concat(template.GenericFieldDefinitions)
                .ToList();

            // For child items (Options/Components), include only fields that have a ParentFieldId
            // For main items (None), include only root-level fields (ParentFieldId == null)
            var applicableFields = hasParent
                ? allFieldDefinitions.Where(f => f.ParentFieldId.HasValue)
                : allFieldDefinitions.Where(f => !f.ParentFieldId.HasValue);

            foreach (var fieldDef in applicableFields)
            {
                // Skip collection-type fields (Options, Files) - they don't need default values
                if (CostEstimateFieldTypeHelper.IsCollectionFieldType(fieldDef.FieldType))
                {
                    continue;
                }

                var fieldValue = new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = fieldDef.Id,
                    CreatedAt = now
                };

                // Set default value for Selected field based on RelationType:
                // - None (main item): BoolValue = true (selected by default, participates in group summing)
                // - Component: BoolValue = true (selected by default, summed into parent item)
                // - Option: BoolValue = false (not selected by default, only one option can be selected)
                if (fieldDef.FieldType == FieldType.ItemSystemSelected)
                {
                    fieldValue.BoolValue = relationType != ItemRelationType.Option;
                }

                fieldValues.Add(fieldValue);
            }

            return fieldValues;
        }

        /// <inheritdoc />
        public List<CostEstimateGroupFieldValue> CreateDefaultGroupFieldValues(
            Guid groupId,
            CostEstimateTemplate template,
            DateTime now)
        {
            var fieldValues = new List<CostEstimateGroupFieldValue>();

            foreach (var fieldDef in template.GroupFieldDefinitions)
            {
                var fieldValue = new CostEstimateGroupFieldValue
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    FieldDefinitionId = fieldDef.Id,
                    CreatedAt = now
                };

                fieldValues.Add(fieldValue);
            }

            return fieldValues;
        }
    }
}
