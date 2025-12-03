using Business.Interfaces.Configurations;

namespace Business.Interfaces.Services
{
    public interface IBlobStorageService
    {
        Task UploadAsync(string containerName, string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
        Task<BlobDownload> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generuje SAS (Shared Access Signature) URI dla bezpośredniego dostępu do bloba
        /// </summary>
        /// <param name="containerName">Nazwa kontenera</param>
        /// <param name="blobName">Nazwa bloba (ścieżka)</param>
        /// <param name="expiresInMinutes">Czas ważności tokenu w minutach (domyślnie 60)</param>
        /// <returns>Pełny URL z SAS tokenem</returns>
        Uri GenerateSasUri(string containerName, string blobName, int expiresInMinutes = 60);
    }

    public sealed class BlobDownload
    {
        public required Stream Content { get; init; }
        public string? ContentType { get; init; }
        public long? ContentLength { get; init; }
    }
}
