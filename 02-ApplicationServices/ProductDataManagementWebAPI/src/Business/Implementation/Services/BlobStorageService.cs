using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services
{
    public sealed class BlobStorageService : IBlobStorageService, IAsyncDisposable
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly BlobStorageSettings settings;
        private readonly ILogger<BlobStorageService> logger;

        public BlobStorageService(IOptions<BlobStorageSettings> options, ILogger<BlobStorageService> logger)
        {
            settings = options.Value;
            this.logger = logger;

            if (string.IsNullOrWhiteSpace(settings.ContainerUrl))
            {
                throw new ArgumentException("BlobStorage:Url is not configured.");
            }

            TokenCredential credential = new DefaultAzureCredential();
            blobServiceClient = new BlobServiceClient(new Uri(settings.ContainerUrl), credential);
        }

        public async Task UploadAsync(string containerName, string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName is required");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("blobName is required");
            }

            ArgumentNullException.ThrowIfNull(content);

            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            BlobClient blob = container.GetBlobClient(blobName);
            BlobUploadOptions options = new();
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            await blob.UploadAsync(content, options, cancellationToken);
            logger.LogInformation("Uploaded blob {BlobName} to container {ContainerName}", blobName, containerName);
        }

        public async Task<BlobDownload> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName is required");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("blobName is required");
            }

            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            Response<BlobDownloadResult> result = await blob.DownloadContentAsync(cancellationToken);
            var props = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

            var stream = new MemoryStream(result.Value.Content.ToArray());
            return new BlobDownload
            {
                Content = stream,
                ContentType = props.ContentType,
                ContentLength = props.ContentLength
            };
        }

        public async ValueTask DisposeAsync()
        {
            // Implement any necessary cleanup here
            await Task.CompletedTask;
        }
    }
}
