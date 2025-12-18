using Business.Interfaces.WebModels.Files;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.Files.UploadProjectFiles
{
    /// <summary>
    /// Command do przesyłania plików do projektu
    /// </summary>
    public record UploadProjectFilesCommand : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid ProjectFilePackageId { get; init; }
        
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
