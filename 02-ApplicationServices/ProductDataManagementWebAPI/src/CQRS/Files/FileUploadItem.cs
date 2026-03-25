using Microsoft.AspNetCore.Http;

namespace CQRS.Files
{
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
