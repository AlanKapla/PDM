using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
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

            TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = false,
                ExcludeVisualStudioCodeCredential = false,
                Diagnostics =
                {
                    IsLoggingEnabled = true,
                    LoggedHeaderNames = { "x-ms-request-id", "x-ms-client-request-id" },
                    LoggedQueryParameters = { "api-version" }
                }
            });

            blobServiceClient = new BlobServiceClient(new Uri(settings.ContainerUrl), credential);
            
            logger.LogInformation("BlobStorageService initialized with account: {AccountName}", blobServiceClient.AccountName);
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

        public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
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

            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            logger.LogInformation("Deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
        }

        public Uri GenerateSasUri(string containerName, string blobName, int expiresInMinutes = 60)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName is required");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("blobName is required");
            }

            try
            {
                logger.LogInformation("Generating SAS URI for blob: {BlobName} in container: {ContainerName}, account: {AccountName}", 
                    blobName, containerName, blobServiceClient.AccountName);

                BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                var expiresOn = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes);

                logger.LogDebug("Requesting User Delegation Key from Azure AD. StartsOn: {StartsOn}, ExpiresOn: {ExpiresOn}", 
                    startsOn, expiresOn);

                // Get User Delegation Key from Azure AD (requires "Storage Blob Data Contributor" role)
                Response<UserDelegationKey> userDelegationKeyResponse = blobServiceClient.GetUserDelegationKey(
                    startsOn: startsOn,
                    expiresOn: expiresOn);

                var userDelegationKey = userDelegationKeyResponse.Value;

                logger.LogDebug("User Delegation Key obtained. SignedOid: {SignedOid}, SignedTid: {SignedTid}", 
                    userDelegationKey.SignedObjectId, userDelegationKey.SignedTenantId);

                // Create SAS with User Delegation Key
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = startsOn,
                    ExpiresOn = expiresOn,
                    ContentDisposition = "attachment" // Wymusza pobieranie zamiast wyświetlania
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate SAS URI using User Delegation Key
                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blob.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
                };

                Uri sasUri = blobUriBuilder.ToUri();

                logger.LogInformation("Successfully generated User Delegation SAS URI for blob {BlobName}, expires at {ExpiresOn}", 
                    blobName, expiresOn);

                return sasUri;
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                logger.LogError(ex, 
                    "Failed to generate SAS token - 403 Forbidden. " +
                    "Account: {AccountName}, Container: {ContainerName}, Blob: {BlobName}. " +
                    "Ensure the Service Principal has 'Storage Blob Data Contributor' or 'Storage Blob Delegator' role. " +
                    "Error Code: {ErrorCode}, Message: {Message}",
                    blobServiceClient.AccountName, containerName, blobName, ex.ErrorCode, ex.Message);

                throw new InvalidOperationException(
                    $"Failed to generate SAS token for blob '{blobName}'. " +
                    $"The Service Principal may not have sufficient permissions on storage account '{blobServiceClient.AccountName}'. " +
                    $"Required role: 'Storage Blob Data Contributor' or 'Storage Blob Delegator'. " +
                    $"Azure Error: {ex.ErrorCode} - {ex.Message}", ex);
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, 
                    "Failed to generate SAS token - HTTP {StatusCode}. " +
                    "Account: {AccountName}, Container: {ContainerName}, Blob: {BlobName}. " +
                    "Error Code: {ErrorCode}, Message: {Message}",
                    ex.Status, blobServiceClient.AccountName, containerName, blobName, ex.ErrorCode, ex.Message);

                throw new InvalidOperationException(
                    $"Failed to generate SAS token for blob '{blobName}'. " +
                    $"Azure Error: {ex.ErrorCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Unexpected error generating SAS token. " +
                    "Account: {AccountName}, Container: {ContainerName}, Blob: {BlobName}",
                    blobServiceClient.AccountName, containerName, blobName);

                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
