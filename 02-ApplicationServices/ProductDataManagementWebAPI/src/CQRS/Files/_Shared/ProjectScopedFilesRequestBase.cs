using Business.Interfaces.Model;

namespace CQRS.Files._Shared
{
    /// <summary>
    /// Base record for Files-domain requests scoped to a tenant + project.
    /// Provides shared TenantId / ProjectId and a default <see cref="ResourceRef"/> built from them.
    /// Derived requests must specify the required permission code.
    /// </summary>
    public abstract record ProjectScopedFilesRequestBase : IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public abstract string PermissionCode { get; }

        public virtual ResourceRef GetResource() =>
            new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
    }
}
