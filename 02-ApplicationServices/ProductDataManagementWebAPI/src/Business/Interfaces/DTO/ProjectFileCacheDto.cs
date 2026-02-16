namespace Business.Interfaces.DTO;

/// <summary>
/// Uproszczony model DTO dla pliku projektu używany w cache
/// </summary>
public record ProjectFileCacheDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ProjectFilePackageId { get; init; }
    public Guid OwnerId { get; init; }
    public string FileName { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid? CurrentVersionId { get; init; }
    public bool IsDeleted { get; init; }
}
