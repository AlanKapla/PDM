using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Interfaces.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationExtractionContext
{
    public IReadOnlyList<DrawingCatalogEntry> Catalog { get; init; } = [];
    public IReadOnlyList<RelatedDrawingRef> RelatedDrawings { get; init; } = [];
}

public sealed class DrawingCatalogEntry
{
    public string FileName { get; init; } = string.Empty;
    public int PageNumber { get; init; }
    public DrawingClassification Classification { get; init; } = new();
}
