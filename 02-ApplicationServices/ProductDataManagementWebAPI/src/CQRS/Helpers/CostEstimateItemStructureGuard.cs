using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;

namespace CQRS.Helpers
{
    /// <summary>
    /// Współdzielone reguły strukturalne dla pozycji kosztorysu.
    /// Używane przez <see cref="CQRS.CostEstimates.UpsertCostEstimateItemField.UpsertCostEstimateItemFieldCommandHandler"/>
    /// i <see cref="CQRS.CostEstimates.UploadCostEstimateFieldFiles.UploadCostEstimateFieldFilesCommandHandler"/>.
    /// </summary>
    internal static class CostEstimateItemStructureGuard
    {
        /// <summary>
        /// Pozycja główna (RelationType=None) z komponentami nie może mieć bezpośrednich FieldValues
        /// (w tym plików). Wartości są zapisywane wyłącznie na komponentach.
        /// </summary>
        internal static void EnsureItemHasNoComponents(
            Guid itemId,
            Dictionary<Guid, CostEstimateItem> itemsDict)
        {
            CostEstimateItem item = itemsDict[itemId];

            if (item.RelationType != ItemRelationType.None)
            {
                return;
            }

            bool hasComponents = itemsDict.Values
                .Any(i => i.ParentItemId == itemId && i.RelationType == ItemRelationType.Component);

            if (hasComponents)
            {
                throw new ValidationApiException(
                    "This item has Components. Items with components cannot have direct field values " +
                    "\u2014 set values on the components instead.");
            }
        }
    }
}
