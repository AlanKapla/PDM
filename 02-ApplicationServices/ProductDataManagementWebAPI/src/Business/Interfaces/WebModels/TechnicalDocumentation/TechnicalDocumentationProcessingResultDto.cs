using Entities.Enums;

namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed record TechnicalDocumentationProcessingResultDto
{
    public required Guid DocumentationId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}
