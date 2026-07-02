using Entities.Enums;

namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed record TechnicalDocumentationListItemWeb
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
    public required int FileCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
