using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.UploadProjectFiles
{
    /// <summary>
    /// Command do przesyłania plików do projektu
    /// </summary>
    public sealed record UploadProjectFilesCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
    {
        public required Guid ProjectFilePackageId { get; init; }

        /// <summary>
        /// Lista plików do przesłania z opcjonalnymi nazwami wyświetlanymi
        /// </summary>
        public List<FileUploadItem> Files { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectFiles;
    }
}
