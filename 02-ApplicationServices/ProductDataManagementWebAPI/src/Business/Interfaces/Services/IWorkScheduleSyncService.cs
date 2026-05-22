using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

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
