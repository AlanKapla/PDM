using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.Files.UploadProjectFileVersion
{
    /// <summary>
    /// Command do przesłania nowej wersji istniejącego pliku projektu
    /// </summary>
    public record UploadProjectFileVersionCommand : IRequest<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid FileId { get; init; }
        
        /// <summary>
        /// Nowy plik - kolejna wersja
        /// </summary>
        public IFormFile File { get; init; } = default!;
        
        /// <summary>
        /// Opcjonalny komentarz do wersji (np. "Poprawiono błędy w sekcji 3")
        /// </summary>
        public string? Comment { get; init; }
    }
}
