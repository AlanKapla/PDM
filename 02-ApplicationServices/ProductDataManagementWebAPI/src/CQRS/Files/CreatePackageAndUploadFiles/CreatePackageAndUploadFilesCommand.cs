using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    /// <summary>
    /// Command to create a new package and upload files to it
    /// </summary>
    public record CreatePackageAndUploadFilesCommand : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public string PackageName { get; init; } = default!;
        
        /// <summary>
        /// Lista plików do przesłania z opcjonalnymi nazwami wyświetlanymi
        /// </summary>
        public List<FileUploadItem> Files { get; init; } = new();
    }

    /// <summary>
    /// Pojedynczy plik z opcjonalną nazwą wyświetlaną i komentarzem
    /// </summary>
    public record FileUploadItem
    {
        public IFormFile File { get; init; } = default!;
        public string? DisplayName { get; init; }
        public string? Comment { get; init; }
    }
}
