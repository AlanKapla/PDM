using Entities.Models.Costs;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Sends notifications about <see cref="ProjectCost"/> sharing changes to affected users.
    /// </summary>
    public interface IProjectCostShareNotificationService
    {
        /// <summary>
        /// Notifies users that a single cost has been shared with them.
        /// </summary>
        Task NotifyCostSharedAsync(
            ProjectCost cost,
            IReadOnlyCollection<Guid> targetUserIds,
            Guid actorUserId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Notifies users about a diff in the share list of a single cost — added users
        /// receive "shared", removed users receive "unshared" notifications.
        /// </summary>
        Task NotifyShareUpdatedAsync(
            ProjectCost cost,
            IReadOnlyCollection<Guid> addedUserIds,
            IReadOnlyCollection<Guid> removedUserIds,
            Guid actorUserId,
            CancellationToken cancellationToken);
    }
}
