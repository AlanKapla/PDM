using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.Files.UploadProjectFileVersion
{
    /// <summary>
    /// Command do przesłania nowej wersji istniejącego pliku projektu
    /// </summary>
    public sealed record UploadProjectFileVersionCommand : FileScopedRequestBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// Nowy plik - kolejna wersja
        /// </summary>
        public required IFormFile File { get; init; }

        /// <summary>
        /// Opcjonalny komentarz do wersji (np. "Poprawiono błędy w sekcji 3")
        /// </summary>
        public string? Comment { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWriteShared;
    }
}
