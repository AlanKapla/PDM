using Azure.Storage.Blobs;
using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Service for storing Excel files for cost estimate import
/// Handles both Azure Blob Storage and database metadata
/// Container: cost-estimates-to-import
/// Path structure: {tenantId}/{projectId}/{userId}/{fileId}_{fileName}
/// </summary>
public sealed class CostEstimateExcelStorageService : ICostEstimateExcelStorageService
{
    private const string ContainerName = "cost-estimates-to-import";
    
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IRepository<CostEstimateFile> _fileRepository;
    private readonly ILogger<CostEstimateExcelStorageService> _logger;

    public CostEstimateExcelStorageService(
        IOptions<BlobStorageSettings> blobStorageSettings,
        IRepository<CostEstimateFile> fileRepository,
        ILogger<CostEstimateExcelStorageService> logger)
    {
        var settings = blobStorageSettings.Value;
        _blobServiceClient = new BlobServiceClient(new Uri(settings.ContainerUrl).GetLeftPart(UriPartial.Authority));
        _fileRepository = fileRepository;
        _logger = logger;
    }

    public async Task<(Stream Stream, string FileName, Guid FileId)> GetOrUploadExcelStreamAsync(
        IFormFile? excelFile,
        Guid? fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        // Scenario 1: Upload new file (IFormFile provided)
        if (excelFile != null)
        {
            var newFileId = Guid.NewGuid();
            
            // Upload to blob + save to DB
            await UploadToBlobAndDbAsync(
                newFileId,
                excelFile,
                tenantId,
                projectId,
                userId,
                displayName,
                cancellationToken);

            // Return fresh stream from uploaded file
            var stream = excelFile.OpenReadStream();
            var fileName = string.IsNullOrWhiteSpace(displayName) ? excelFile.FileName : displayName;

            _logger.LogInformation(
                "Uploaded Excel file. FileId: {FileId}, FileName: {FileName}",
                newFileId, excelFile.FileName);

            return (stream, fileName, newFileId);
        }

        // Scenario 2: Get existing file from DB + blob (FileId provided)
        if (fileId.HasValue)
        {
            // Get file metadata from database
            var file = await GetFileFromDbAsync(fileId.Value, tenantId, projectId, userId);

            // Get stream from blob storage
            var stream = await GetStreamFromBlobAsync(file.BlobPath, cancellationToken);

            _logger.LogInformation(
                "Retrieved Excel file from storage. FileId: {FileId}, FileName: {FileName}",
                fileId.Value, file.DisplayName);

            return (stream, file.DisplayName, fileId.Value);
        }

        throw new ValidationApiException("Either excelFile or fileId must be provided");
    }

    public async Task<(Stream Stream, string FileName)> GetExcelStreamAsync(
        Guid fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get file from database
        var file = await GetFileFromDbAsync(fileId, tenantId, projectId, userId);

        // Get stream from blob storage
        var stream = await GetStreamFromBlobAsync(file.BlobPath, cancellationToken);

        return (stream, file.OriginalFileName);
    }

    public async Task<List<ExcelImportFileMetadata>> GetExcelFilesAsync(
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get files from database
        var files = await _fileRepository.GetBySearch(
            f => f.TenantId == tenantId &&
                 f.ProjectId == projectId &&
                 f.OwnerId == userId &&
                 !f.IsDeleted);

        return files.Select(f => new ExcelImportFileMetadata(
            FileId: f.Id.ToString(),
            FileName: f.DisplayName,
            BlobUrl: f.BlobUrl,
            SizeBytes: f.FileSizeBytes,
            UploadedAt: f.UploadedAt
        )).ToList();
    }

    public async Task DeleteExcelAsync(
        Guid fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get file from database
        var file = await GetFileFromDbAsync(fileId, tenantId, projectId, userId);

        // Soft delete in database
        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        await _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Soft deleted Excel file. FileId: {FileId}",
            fileId);
    }

    // ====== PRIVATE HELPER METHODS ======

    private async Task UploadToBlobAndDbAsync(
        Guid fileId,
        IFormFile file,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        string? displayName,
        CancellationToken cancellationToken)
    {
        // Upload to blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = $"{tenantId}/{projectId}/{userId}/{fileId}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        await using var uploadStream = file.OpenReadStream();
        await blobClient.UploadAsync(uploadStream, overwrite: true, cancellationToken);

        // Save metadata to database
        var now = DateTime.UtcNow;
        var finalDisplayName = string.IsNullOrWhiteSpace(displayName) ? file.FileName : displayName;

        var fileEntity = new CostEstimateFile
        {
            Id = fileId,
            TenantId = tenantId,
            ProjectId = projectId,
            OwnerId = userId,
            DisplayName = finalDisplayName,
            OriginalFileName = file.FileName,
            FileSizeBytes = file.Length,
            BlobPath = blobPath,
            BlobUrl = blobClient.Uri.ToString(),
            UploadedAt = now,
            IsDeleted = false
        };

        await _fileRepository.Insert(fileEntity);
        await _fileRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CostEstimateFile> GetFileFromDbAsync(
        Guid fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId)
    {
        var file = await _fileRepository.GetFirstBySearch(
            f => f.Id == fileId &&
                 f.TenantId == tenantId &&
                 f.ProjectId == projectId &&
                 f.OwnerId == userId &&
                 !f.IsDeleted);

        if (file == null)
            throw new NotFoundApiException("CostEstimateFile", fileId.ToString());

        return file;
    }

    private async Task<Stream> GetStreamFromBlobAsync(
        string blobPath,
        CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(cancellationToken))
            throw new FileNotFoundException($"Excel file not found in blob storage: {blobPath}");

        var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream, cancellationToken);
        stream.Position = 0;

        return stream;
    }
}


