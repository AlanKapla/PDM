using Business.Interfaces.Configurations;

namespace Business.Interfaces.Services
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Upload pliku do Azure Blob Storage
        /// </summary>
        /// <param name="containerName">Nazwa kontenera</param>
        /// <param name="blobName">Nazwa bloba (ścieżka w storage)</param>
        /// <param name="content">Stream z zawartością pliku</param>
        /// <param name="contentType">Typ MIME pliku</param>
        /// <param name="cancellationToken">Token anulowania</param>
        Task UploadAsync(string containerName, string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
        
        Task<BlobDownload> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generuje SAS (Shared Access Signature) URI dla bezpośredniego dostępu do bloba
        /// </summary>
        /// <param name="containerName">Nazwa kontenera</param>
        /// <param name="blobName">Nazwa bloba (ścieżka)</param>
        /// <param name="fileName">Nazwa pliku do wyświetlenia (display name) w Content-Disposition</param>
        /// <param name="expiresInMinutes">Czas ważności tokenu w minutach (domyślnie 60)</param>
        /// <param name="contentDisposition">Sposób obsługi pliku: "inline" (wyświetlanie) lub "attachment" (pobieranie)</param>
        /// <returns>Pełny URL z SAS tokenem</returns>
        Uri GenerateSasUri(string containerName, string blobName, string fileName, int expiresInMinutes = 60, string contentDisposition = "inline");
        
        /// <summary>
        /// Aktualizuje Content-Disposition metadata dla istniejącego bloba
        /// Używane do migracji starych blobów bez poprawnej metadaty
        /// </summary>
        /// <param name="containerName">Nazwa kontenera</param>
        /// <param name="blobName">Nazwa bloba (ścieżka)</param>
        /// <param name="contentDisposition">Nowa wartość Content-Disposition (np. "inline; filename=\"file.pdf\"")</param>
        /// <param name="cancellationToken">Token anulowania</param>
        /// <returns>True jeśli metadata została zaktualizowana</returns>
        Task<bool> UpdateBlobContentDispositionAsync(string containerName, string blobName, string contentDisposition, CancellationToken cancellationToken = default);
    }

    public sealed class BlobDownload
    {
        public required Stream Content { get; init; }
        public string? ContentType { get; init; }
        public long? ContentLength { get; init; }
    }
}
