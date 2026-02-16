namespace Business.Interfaces.DTO;

/// <summary>
/// Uproszczony model DTO dla komentarza wersji pliku projektu używany w cache
/// </summary>
public record ProjectFileVersionCommentDto
{
    public Guid Id { get; init; }
    public Guid ProjectFileVersionId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public string Content { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public bool IsDeleted { get; init; }
}
