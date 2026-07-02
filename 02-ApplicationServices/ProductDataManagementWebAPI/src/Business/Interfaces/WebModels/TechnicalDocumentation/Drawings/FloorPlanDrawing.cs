using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class FloorPlanDrawing
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DrawingSource Source { get; set; } = new();
    public DrawingClassification Classification { get; set; } = new();
    public double? TotalAreaM2 { get; set; }
    public string? AreaNotes { get; set; }
    public DrawingExternalDimensions? ExternalDimensions { get; set; }
    public List<Room> Rooms { get; set; } = new();
    public List<Wall> Walls { get; set; } = new();
    public List<Opening> Openings { get; set; } = new();
    public List<StructuralColumn> Columns { get; set; } = new();
    public List<StructuralBeam> Beams { get; set; } = new();
    public List<StructuralLintel> Lintels { get; set; } = new();
    public FoundationSection? Foundations { get; set; }
    public FloorSection? Floors { get; set; }
    public RoofSection? Roof { get; set; }
    public SitePlanSection? Site { get; set; }
    public SectionDrawingData? Section { get; set; }
    public ElevationDrawingData? Elevation { get; set; }
    public List<StructuralDetail> Details { get; set; } = new();
    public List<DrawingInstallation> Installations { get; set; } = new();
    public List<InteriorDoorEntry> InteriorDoors { get; set; } = new();
    public DrawingTextSources? TextSources { get; set; }
    public List<DrawingCrossReference> CrossReferences { get; set; } = new();
    public List<DeferredDetailNote> DeferredDetails { get; set; } = new();
    public ValidationReport? ValidationReport { get; set; }
}
