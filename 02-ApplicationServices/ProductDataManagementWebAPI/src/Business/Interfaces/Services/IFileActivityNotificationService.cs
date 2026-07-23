namespace Business.Interfaces.Services
{
    /// <summary>
    /// Notifies file owner and shared users about file activity (new version / comment).
    /// Failures must not propagate — the activity is already persisted before notifications run.
    /// </summary>
    public interface IFileActivityNotificationService
    {
        Task NotifyCommentAddedAsync(FileActivityNotificationContext context, CancellationToken cancellationToken);

        Task NotifyVersionUploadedAsync(FileActivityNotificationContext context, CancellationToken cancellationToken);
    }

    public sealed record FileActivityNotificationContext
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid FileId { get; init; }
        public required Guid PackageId { get; init; }
        public required Guid OwnerId { get; init; }
        public required string FileDisplayName { get; init; }
        public required string ActorName { get; init; }
        public required Guid ActorUserId { get; init; }
        public Guid? VersionId { get; init; }
        public Guid? CommentId { get; init; }
    }
}
