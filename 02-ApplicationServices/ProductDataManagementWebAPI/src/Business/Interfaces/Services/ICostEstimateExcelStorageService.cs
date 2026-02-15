using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Services;

/// <summary>
/// Service for storing Excel files for cost estimate import
/// Handles both blob storage and database metadata
/// </summary>
public interface ICostEstimateExcelStorageService
{
    /// <summary>
    /// Get or upload Excel stream for parsing
    /// If excelFile provided → uploads to blob + DB, returns stream
    /// If fileId provided → gets from DB + blob, returns stream
    /// Always returns stream ready for parsing
    /// </summary>
    /// <returns>Tuple of (Stream, FileName, FileId)</returns>
    Task<(Stream Stream, string FileName, Guid FileId)> GetOrUploadExcelStreamAsync(
        IFormFile? excelFile,
        Guid? fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List Excel files uploaded by user for a project (from database)
    /// </summary>
    Task<List<ExcelImportFileMetadata>> GetExcelFilesAsync(
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete Excel file - soft delete in database
    /// </summary>
    Task DeleteExcelAsync(
        Guid fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Excel stream by FileId (for download purposes)
    /// </summary>
    Task<(Stream Stream, string FileName)> GetExcelStreamAsync(
        Guid fileId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata for uploaded Excel file
/// </summary>
public record ExcelImportFileMetadata(
    string FileId,
    string FileName,
    string BlobUrl,
    long SizeBytes,
    DateTime UploadedAt
);
