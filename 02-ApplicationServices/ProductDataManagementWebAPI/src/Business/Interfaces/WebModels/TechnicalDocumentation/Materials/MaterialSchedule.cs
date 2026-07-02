namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class MaterialSchedule
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public List<string> DrawingsUsed { get; set; } = new();
    public List<string> MissingDrawings { get; set; } = new();
    public List<string> MissingDimensions { get; set; } = new();
    public List<MaterialItem> Masonry { get; set; } = new();
    public List<MaterialItem> Insulation { get; set; } = new();
    public List<MaterialItem> Concrete { get; set; } = new();
    public List<MaterialItem> Steel { get; set; } = new();
    public List<MaterialItem> Timber { get; set; } = new();
    public List<MaterialItem> Roofing { get; set; } = new();
    public List<MaterialItem> Finishes { get; set; } = new();
    public FoundationMaterials Foundations { get; set; } = new();
    public WallMaterials Walls { get; set; } = new();
    public CeilingMaterials Ceilings { get; set; } = new();
    public ColumnMaterials Columns { get; set; } = new();
    public RoofMaterials Roof { get; set; } = new();
    public List<OpeningScheduleItem> Openings { get; set; } = new();
    public List<MaterialSummaryItem> Summary { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
