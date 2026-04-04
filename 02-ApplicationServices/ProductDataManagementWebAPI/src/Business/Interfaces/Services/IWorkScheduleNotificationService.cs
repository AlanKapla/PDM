namespace Business.Interfaces.Services
{
    /// <summary>
    /// Sends work schedule assignment notifications to affected users.
    /// </summary>
    public interface IWorkScheduleNotificationService
    {
        /// <summary>
        /// Sends "assigned to work schedule" notifications to all given users (used on creation).
        /// </summary>
        Task SendAssignmentCreatedNotificationsAsync(
            IEnumerable<Guid> userIds,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Sends "assigned" or "removed from work schedule" notifications based on the diff between previous and current assignments (used on update).
        /// </summary>
        Task SendAssignmentChangedNotificationsAsync(
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken);
    }
}
