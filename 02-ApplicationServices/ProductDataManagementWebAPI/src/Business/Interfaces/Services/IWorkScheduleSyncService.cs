using Entities.Models;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Synchronizes work schedule stages with the linked cost estimate group hierarchy.
    /// </summary>
    public interface IWorkScheduleSyncService
    {
        /// <summary>
        /// Syncs stages of the given work schedule with its linked cost estimate group tree.
        /// Creates stages for new groups, updates names/order for existing ones,
        /// and soft-deletes stages whose groups have been removed.
        /// Returns the flat list of all active (non-deleted) linked stages after sync.
        /// </summary>
        Task<List<WorkScheduleStage>> SyncFromCostEstimateAsync(
            WorkSchedule workSchedule,
            CancellationToken cancellationToken);
    }
}
