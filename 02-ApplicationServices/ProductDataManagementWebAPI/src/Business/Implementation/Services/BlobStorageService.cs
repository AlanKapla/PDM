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
using Business.Interfaces.Helpers;
using Business.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services
{
    public sealed class BlobStorageService : IBlobStorageService, IAsyncDisposable
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly BlobStorageSettings settings;
        private readonly ILogger<BlobStorageService> logger;
        private readonly IMemoryCache memoryCache;

        private const string CacheKeyPrefix = "UserDelegationKey_";

        public BlobStorageService(
            IOptions<BlobStorageSettings> options, 
            ILogger<BlobStorageService> logger,
            IMemoryCache memoryCache)
        {
            settings = options.Value;
            this.logger = logger;
            this.memoryCache = memoryCache;

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

        public Uri GenerateSasUri(string containerName, string blobName, string fileName, int expiresInMinutes = 60, string contentDisposition = "inline")
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName is required");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("blobName is required");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName is required");
            }

            try
            {
                // Normalize fileName for safe use in Content-Disposition header
                string normalizedFileName = FileHelper.NormalizeFileNameForContentDisposition(fileName);

                BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                // FIXED: Normalize expiration time to 15-minute blocks to maximize cache hits
                // This prevents cache stampede when calls happen at minute boundaries
                var now = DateTimeOffset.UtcNow;
                var startsOn = now.AddMinutes(-5);
                var expiresOn = NormalizeToBlock(now.AddMinutes(expiresInMinutes), minutes: 15);

                // Użyj cache dla User Delegation Key (IMemoryCache - thread-safe, automatyczne wygasanie)
                var userDelegationKey = GetOrCreateUserDelegationKey(startsOn, expiresOn);
                
                // Prosty format Content-Disposition bez RFC 5987 encoding
                string fullContentDisposition = $"{contentDisposition}; filename=\"{normalizedFileName}\"";
                
                // Create SAS with User Delegation Key
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = startsOn,
                    ExpiresOn = expiresOn,
                    ContentDisposition = fullContentDisposition
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate SAS URI using User Delegation Key
                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blob.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
                };

                Uri sasUri = blobUriBuilder.ToUri();

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
            // IMemoryCache jest zarządzany przez DI container - nie wymaga ręcznego Dispose
            await Task.CompletedTask;
        }

        /// <summary>
        /// Normalizuje DateTimeOffset do bloków (np. 15 minut) aby maksymalizować cache hits.
        /// Przykład: 10:07 → 10:15, 10:22 → 10:30
        /// </summary>
        private static DateTimeOffset NormalizeToBlock(DateTimeOffset dateTime, int minutes)
        {
            var totalMinutes = dateTime.Minute + (dateTime.Hour * 60);
            var normalizedMinutes = (int)Math.Ceiling(totalMinutes / (double)minutes) * minutes;
            
            var hours = normalizedMinutes / 60;
            var mins = normalizedMinutes % 60;
            
            return new DateTimeOffset(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                hours,
                mins,
                0,
                dateTime.Offset);
        }

        public async Task<bool> UpdateBlobContentDispositionAsync(string containerName, string blobName, string contentDisposition, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName is required");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("blobName is required");
            }

            if (string.IsNullOrWhiteSpace(contentDisposition))
            {
                throw new ArgumentException("contentDisposition is required");
            }

            try
            {
                BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                // Sprawdź czy blob istnieje
                bool exists = await blob.ExistsAsync(cancellationToken);
                if (!exists)
                {
                    logger.LogWarning("Blob {BlobName} does not exist in container {ContainerName}", blobName, containerName);
                    return false;
                }

                // Pobierz obecne właściwości bloba
                BlobProperties properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

                // Ustaw nową wartość Content-Disposition zachowując pozostałe headers
                BlobHttpHeaders headers = new BlobHttpHeaders
                {
                    ContentType = properties.ContentType,
                    ContentEncoding = properties.ContentEncoding,
                    ContentLanguage = properties.ContentLanguage,
                    ContentDisposition = contentDisposition,
                    CacheControl = properties.CacheControl
                };

                // Aktualizuj headers bloba
                await blob.SetHttpHeadersAsync(headers, cancellationToken: cancellationToken);

                logger.LogInformation("Updated Content-Disposition for blob {BlobName}: {ContentDisposition}", 
                    blobName, contentDisposition);

                return true;
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, 
                    "Failed to update Content-Disposition for blob {BlobName}. Status: {Status}, ErrorCode: {ErrorCode}",
                    blobName, ex.Status, ex.ErrorCode);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error updating Content-Disposition for blob {BlobName}", blobName);
                throw;
            }
        }

        /// <summary>
        /// Pobiera User Delegation Key z cache lub tworzy nowy jeśli cache wygasł.
        /// Thread-safe dzięki IMemoryCache (wbudowana synchronizacja).
        /// </summary>
        private UserDelegationKey GetOrCreateUserDelegationKey(DateTimeOffset startsOn, DateTimeOffset expiresOn)
        {
            // Klucz cache bazujący na czasie wygaśnięcia (aby różne TTL miały osobne cache)
            string cacheKey = $"{CacheKeyPrefix}{expiresOn:yyyyMMddHHmm}";

            return memoryCache.GetOrCreate(cacheKey, entry =>
            {
                logger.LogDebug("Cache miss - requesting new User Delegation Key from Azure AD. StartsOn: {StartsOn}, ExpiresOn: {ExpiresOn}", 
                    startsOn, expiresOn);

                // Pobierz nowy User Delegation Key z Azure AD
                Response<UserDelegationKey> userDelegationKeyResponse = blobServiceClient.GetUserDelegationKey(
                    startsOn: startsOn,
                    expiresOn: expiresOn);

                var userDelegationKey = userDelegationKeyResponse.Value;

                // Konfiguracja cache - wygasa 5 minut przed rzeczywistym wygaśnięciem klucza (safety buffer)
                entry.AbsoluteExpiration = expiresOn.AddMinutes(-5);
                entry.Priority = CacheItemPriority.High; // Wysoki priorytet - rzadko używany, ale ważny

                logger.LogInformation("New User Delegation Key cached until {CacheExpiration} (key expires: {KeyExpiration}). SignedOid: {SignedOid}, SignedTid: {SignedTid}",
                    entry.AbsoluteExpiration, expiresOn, userDelegationKey.SignedObjectId, userDelegationKey.SignedTenantId);

                return userDelegationKey;
            })!; // Non-null bo GetOrCreate zawsze zwraca wartość (factory nie może zwrócić null)
        }
    }
}
