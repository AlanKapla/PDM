namespace Business.Interfaces.DTO;

/// <summary>
/// Uproszczony model DTO dla paczki plików projektu używany w cache
/// </summary>
public record ProjectFilePackageDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public bool IsDeleted { get; init; }
}
