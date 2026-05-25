namespace CQRS.Files._Shared
{
    /// <summary>
    /// Base record for Files-domain requests targeting a single project file.
    /// Inherits TenantId / ProjectId from <see cref="ProjectScopedFilesRequestBase"/>
    /// and adds the required FileId.
    /// </summary>
    public abstract record FileScopedRequestBase : ProjectScopedFilesRequestBase
    {
        public Guid FileId { get; init; }
    }
}
