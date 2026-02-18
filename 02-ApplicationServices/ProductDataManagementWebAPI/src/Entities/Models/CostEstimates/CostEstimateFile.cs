using Entities.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.CostEstimates;

/// <summary>
/// Excel file uploaded for cost estimate import
/// Stored in blob storage with metadata in database
/// </summary>
public sealed class CostEstimateFile : BaseEntity
{
    /// <summary>
    /// Tenant ID (multi-tenancy)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Owner (uploader) ID
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// User-friendly display name (editable)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Original file name (e.g., "kosztorys.xlsx")
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Blob storage path (e.g., "{tenantId}/{projectId}/{userId}/{guid}_{fileName}")
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Full blob URL
    /// </summary>
    public string BlobUrl { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
