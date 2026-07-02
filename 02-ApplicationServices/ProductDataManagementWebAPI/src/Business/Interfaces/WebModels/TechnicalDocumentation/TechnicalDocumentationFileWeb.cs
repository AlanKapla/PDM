namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed record TechnicalDocumentationFileWeb
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public string? SasUriPreview { get; init; }
    public string? SasUriDownload { get; init; }
}
