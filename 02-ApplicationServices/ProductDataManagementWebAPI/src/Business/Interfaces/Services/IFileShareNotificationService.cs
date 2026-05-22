namespace Business.Interfaces.Services
{
    /// <summary>
    /// Sends notifications to users whose access to a file has changed.
    /// Failures must not propagate — sharing is already persisted before notifications run.
    /// </summary>
    public interface IFileShareNotificationService
    {
        Task NotifyShareGrantedAsync(FileShareNotificationContext context, CancellationToken cancellationToken);
        Task NotifyShareRevokedAsync(FileShareNotificationContext context, CancellationToken cancellationToken);
    }

    public sealed record FileShareNotificationContext
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid FileId { get; init; }
        public required string FileDisplayName { get; init; }
        public required string OwnerName { get; init; }
        public required IReadOnlyCollection<Guid> UserIds { get; init; }
    }
}
