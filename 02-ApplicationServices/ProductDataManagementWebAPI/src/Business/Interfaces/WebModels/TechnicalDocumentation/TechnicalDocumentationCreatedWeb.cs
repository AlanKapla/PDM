using Entities.Enums;

namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed record TechnicalDocumentationCreatedWeb
{
    public required Guid Id { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
}
