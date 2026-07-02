namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class DrawingClassification
{
    public string DrawingType { get; set; } = string.Empty;
    public int? Scale { get; set; }
    public string? SheetNumber { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Date { get; set; }
    public string? Investor { get; set; }
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? Collaborator { get; set; }
    public string? Phase { get; set; }
    public string? ProjectName { get; set; }
    public string? BuildingType { get; set; }
    public string? Revision { get; set; }
    public string? DescriptiveText { get; set; }
    public string? DrawingTable { get; set; }
    public string? ElementAnnotations { get; set; }
    public string? TableContent { get; set; }
    public string? TechnicalParameters { get; set; }
    public string? Legend { get; set; }
    public string? Notes { get; set; }
    public string? FloorLevel { get; set; }
    public int? FloorOrder { get; set; }
    public bool HasMaterialTable { get; set; }
    public string? TableTitle { get; set; }
    public List<RelatedDrawingRef> RelatedDrawings { get; set; } = new();
}
