namespace Business.Interfaces.DTO;

/// <summary>
/// Uproszczony model DTO dla wersji pliku projektu używany w cache
/// </summary>
public record ProjectFileVersionDto
{
    public Guid Id { get; init; }
    public Guid ProjectFileId { get; init; }
    public int VersionNumber { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string BlobFileName { get; init; } = default!;
    public string BlobPath { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long FileSizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsDeleted { get; init; }
}
