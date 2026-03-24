using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Service for managing cost estimate data operations
    /// </summary>
    public interface ICostEstimateService
    {
        /// <summary>
        /// Adds ItemSystemSelected field value (BoolValue = true) to all existing items
        /// in cost estimates that use the specified template.
        /// Called when the template gains the ItemSystemSelected field definition,
        /// so existing items are automatically marked as selected.
        /// </summary>
        Task AddSelectedFieldToExistingItemsAsync(
            Guid templateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates default empty field values for a new item based on template definitions.
        /// Skips collection-type fields (Options, Files) and creates values with null/default for all others.
        /// For Options/Components, only includes fields that have a ParentFieldId matching an Options field.
        /// </summary>
        /// <param name="itemId">The ID of the newly created item</param>
        /// <param name="template">Template with loaded SystemFieldDefinitions, CalculatedFieldDefinitions, GenericFieldDefinitions</param>
        /// <param name="relationType">The relation type of the item (None, Option, Component)</param>
        /// <param name="hasParent">Whether the item has a parent item</param>
        /// <param name="now">Timestamp for CreatedAt</param>
        /// <returns>List of created field values with empty/default values</returns>
        List<CostEstimateItemFieldValue> CreateDefaultItemFieldValues(
            Guid itemId,
            CostEstimateTemplate template,
            ItemRelationType relationType,
            bool hasParent,
            DateTime now);

        /// <summary>
        /// Creates default empty field values for a new group based on template definitions.
        /// </summary>
        /// <param name="groupId">The ID of the newly created group</param>
        /// <param name="template">Template with loaded GroupFieldDefinitions</param>
        /// <param name="now">Timestamp for CreatedAt</param>
        /// <returns>List of created group field values with empty/default values</returns>
        List<CostEstimateGroupFieldValue> CreateDefaultGroupFieldValues(
            Guid groupId,
            CostEstimateTemplate template,
            DateTime now);
    }
}
