namespace Business.Interfaces.Services
{
    /// <summary>
    /// Kind of access being requested for a file or package resource.
    /// </summary>
    public enum FileAccessKind
    {
        Read,
        Write,
        Share,
        Delete,
    }

    /// <summary>
    /// Centralized authorization guard for ProjectFile / ProjectFilePackage operations.
    /// Encapsulates the rule: tenant/project admin OR resource owner OR (for Read/Write on file) user with share access.
    /// Throws <see cref="Business.Interfaces.Exceptions.NotFoundApiException"/> when the resource does not exist
    /// and <see cref="Business.Interfaces.Exceptions.ForbiddenApiException"/> when the caller is not allowed.
    /// </summary>
    public interface IFileAccessGuard
    {
        Task EnsureCanAccessFileAsync(
            Guid tenantId,
            Guid projectId,
            Guid fileId,
            FileAccessKind kind,
            CancellationToken cancellationToken);

        Task EnsureCanAccessPackageAsync(
            Guid tenantId,
            Guid projectId,
            Guid packageId,
            FileAccessKind kind,
            CancellationToken cancellationToken);
    }
}
