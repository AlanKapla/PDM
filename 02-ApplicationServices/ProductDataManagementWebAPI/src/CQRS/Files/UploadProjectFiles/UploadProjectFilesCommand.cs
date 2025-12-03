using Business.Interfaces.WebModels.Files;
using Microsoft.AspNetCore.Http;

namespace CQRS.Files.UploadProjectFiles
{
    /// <summary>
    /// Command do przesyłania plików do projektu
    /// </summary>
    public record UploadProjectFilesCommand : IRequestCommand<List<ProjectFileWeb>>
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
    /// Pojedynczy plik z opcjonalną nazwą wyświetlaną
    /// </summary>
    public record FileUploadItem
    {
        public IFormFile File { get; init; } = default!;
        public string? DisplayName { get; init; }
    }
}
