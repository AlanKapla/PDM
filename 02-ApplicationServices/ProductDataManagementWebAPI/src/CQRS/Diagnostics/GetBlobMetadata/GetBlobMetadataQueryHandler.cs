using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CQRS.Diagnostics.GetBlobMetadata
{
    /// <summary>
    /// Handler do pobierania metadanych bloba (diagnostyka Content-Disposition)
    /// </summary>
    public class GetBlobMetadataQueryHandler : IRequestHandler<GetBlobMetadataQuery, BlobMetadataResult>
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<GetBlobMetadataQueryHandler> logger;

        public GetBlobMetadataQueryHandler(
            IBlobStorageService blobStorageService,
            ILogger<GetBlobMetadataQueryHandler> logger)
        {
            this.blobStorageService = blobStorageService;
            this.logger = logger;
        }

        public async Task<BlobMetadataResult> Handle(GetBlobMetadataQuery request, CancellationToken cancellationToken)
        {
            try
            {
                string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
                
                // Potrzebujemy bezpośredniego dostępu do BlobClient
                // Dodamy pomocniczą metodę do IBlobStorageService lub użyjemy GetPropertiesAsync bezpośrednio
                
                logger.LogInformation("Getting blob metadata for: {BlobPath}", request.BlobPath);

                // TODO: Rozszerzyć IBlobStorageService o GetBlobPropertiesAsync
                // Na razie zwróć placeholder
                return new BlobMetadataResult
                {
                    BlobPath = request.BlobPath,
                    Exists = false
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting blob metadata for {BlobPath}", request.BlobPath);
                throw;
            }
        }
    }
}
