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
        /// Systemowe pola meta (ItemSystemName, ItemSystemSelected) są wyjątkiem — opisują samą pozycję,
        /// a nie jej wartości do kalkulacji.
        /// </summary>
        internal static void EnsureItemHasNoComponents(
            Guid itemId,
            Dictionary<Guid, CostEstimateItem> itemsDict,
            FieldType? fieldType = null)
        {
            CostEstimateItem item = itemsDict[itemId];

            if (item.RelationType != ItemRelationType.None)
            {
                return;
            }

            // Systemowe pola meta są dozwolone na pozycji z komponentami:
            // - ItemSystemName (100) — nazwa pozycji
            // - ItemSystemSelected (104) — zaznaczenie do sumowania
            if (fieldType.HasValue &&
                (fieldType.Value == FieldType.ItemSystemName ||
                 fieldType.Value == FieldType.ItemSystemSelected))
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
