using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Command do dodawania komentarza do konkretnej wersji pliku
    /// </summary>
    public record AddFileVersionCommentCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        /// <summary>
        /// ID tenanta (z route)
        /// </summary>
        public Guid TenantId { get; init; }

        /// <summary>
        /// ID projektu (z route)
        /// </summary>
        public Guid ProjectId { get; init; }

        /// <summary>
        /// ID pliku (z route)
        /// </summary>
        public Guid FileId { get; init; }

        /// <summary>
        /// ID wersji pliku (z route)
        /// </summary>
        public Guid VersionId { get; init; }

        /// <summary>
        /// Treść komentarza
        /// </summary>
        public string Comment { get; init; } = string.Empty;

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
