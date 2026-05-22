using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    /// <summary>
    /// Command to create a new package and upload files to it
    /// </summary>
    public sealed record CreatePackageAndUploadFilesCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
    {
        public required string PackageName { get; init; }

        /// <summary>
        /// Lista plików do przesłania z opcjonalnymi nazwami wyświetlanymi
        /// </summary>
        public List<FileUploadItem> Files { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
