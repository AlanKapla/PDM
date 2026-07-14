using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Command do dodawania komentarza do konkretnej wersji pliku
    /// </summary>
    public sealed record AddFileVersionCommentCommand : FileScopedRequestBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// ID wersji pliku (z route)
        /// </summary>
        public required Guid VersionId { get; init; }

        /// <summary>
        /// Treść komentarza
        /// </summary>
        public required string Comment { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectFiles;
    }
}
