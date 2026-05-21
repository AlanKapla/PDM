using Business.Interfaces.Exceptions;

namespace CQRS.WorkSchedules.Shared
{
    /// <summary>
    /// Shared helpers for re-indexing the <c>Order</c> property of WorkSchedule sibling entities
    /// (stages and works). Centralizes the validation + sequential reassignment pattern used by
    /// the Reorder commands so that both handlers stay symmetrical and free of duplicated logic.
    /// </summary>
    public static class WorkScheduleOrderHelper
    {
        /// <summary>
        /// Validates that <paramref name="orderedIds"/> contains exactly the keys present in
        /// <paramref name="entityMap"/> (no duplicates, no extras, no omissions) and assigns
        /// 0-based sequential <c>Order</c> values via <paramref name="orderSetter"/>.
        /// Returns the entities in the requested order, ready for a bulk update.
        /// </summary>
        /// <exception cref="ValidationApiException">
        /// Thrown when the provided ID set does not match the entity map exactly.
        /// </exception>
        public static List<T> ReassignSequentialOrders<T>(
            IList<Guid> orderedIds,
            IDictionary<Guid, T> entityMap,
            Action<T, int> orderSetter,
            string mismatchMessage)
        {
            HashSet<Guid> orderedSet = orderedIds.ToHashSet();

            if (orderedIds.Count != entityMap.Count
                || orderedSet.Count != entityMap.Count
                || !orderedSet.SetEquals(entityMap.Keys))
            {
                throw new ValidationApiException(mismatchMessage);
            }

            List<T> updated = new List<T>(orderedIds.Count);
            for (int i = 0; i < orderedIds.Count; i++)
            {
                T entity = entityMap[orderedIds[i]];
                orderSetter(entity, i);
                updated.Add(entity);
            }

            return updated;
        }
    }
}
