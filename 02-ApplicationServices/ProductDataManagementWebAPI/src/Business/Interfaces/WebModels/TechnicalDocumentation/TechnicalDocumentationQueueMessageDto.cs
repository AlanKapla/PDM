namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed record TechnicalDocumentationQueueMessageDto
{
    public required Guid DocumentationId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required bool IsManualRetry { get; init; }
}
