using Business.Interfaces.Configurations;

namespace Business.Interfaces.Services
{
    public interface IBlobStorageService
    {
        Task UploadAsync(string containerName, string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
        Task<BlobDownload> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    }

    public sealed class BlobDownload
    {
        public required Stream Content { get; init; }
        public string? ContentType { get; init; }
        public long? ContentLength { get; init; }
    }
}
