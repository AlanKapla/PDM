using Entities.Models.Files;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Pure (no I/O) computation of changes required to satisfy a target list of users
    /// who should have access to a single <see cref="ProjectFile"/> within its package.
    /// </summary>
    public interface IFileShareDiffService
    {
        FileShareDiffResult Compute(FileShareDiffInput input);
    }

    /// <summary>
    /// Input for <see cref="IFileShareDiffService.Compute"/>. Contains only data already
    /// loaded by the caller — no repositories, no DB access here.
    /// </summary>
    public sealed record FileShareDiffInput
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid PackageId { get; init; }
        public required Guid FileId { get; init; }
        public required Guid CurrentUserId { get; init; }

        /// <summary>All existing share rows for the package (file-level + package-level).</summary>
        public required IReadOnlyCollection<SharedProjectFile> ExistingPackageShares { get; init; }

        /// <summary>Target user ids that should have access to the file after the operation.</summary>
        public required IReadOnlyCollection<Guid> TargetUserIds { get; init; }
    }

    /// <summary>
    /// Result of the diff computation: row-level operations to apply
    /// and the user lists that effectively gained or lost access (used for notifications).
    /// </summary>
    public sealed record FileShareDiffResult
    {
        public required IReadOnlyCollection<SharedProjectFile> SharesToInsert { get; init; }
        public required IReadOnlyCollection<SharedProjectFile> SharesToDelete { get; init; }
        public required IReadOnlyCollection<Guid> UsersGrantedAccess { get; init; }
        public required IReadOnlyCollection<Guid> UsersRevokedAccess { get; init; }
    }
}
