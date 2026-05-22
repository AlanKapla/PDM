namespace CQRS.Files._Shared
{
    /// <summary>
    /// Base record for Files-domain requests targeting a single file package.
    /// Inherits TenantId / ProjectId from <see cref="ProjectScopedFilesRequestBase"/>
    /// and adds the required PackageId.
    /// </summary>
    public abstract record PackageScopedRequestBase : ProjectScopedFilesRequestBase
    {
        public required Guid PackageId { get; init; }
    }
}
