namespace Business.Interfaces.WebModels.TechnicalDocumentation.Models;

public sealed class ProjectModel
{
    public ProjectModelMetadata Project { get; set; } = new();
    public ProjectModelSite Site { get; set; } = new();
    public List<ProjectModelFloor> Floors { get; set; } = new();
    public ProjectModelWalls Walls { get; set; } = new();
    public ProjectModelFoundations Foundations { get; set; } = new();
    public ProjectModelSlab? Slab { get; set; }
    public List<ProjectModelCeiling> Ceilings { get; set; } = new();
    public ProjectModelRoof Roof { get; set; } = new();
    public List<ProjectModelElevation> Elevations { get; set; } = new();
    public List<ProjectModelColumn> Columns { get; set; } = new();
    public List<ProjectModelBeam> Beams { get; set; } = new();
    public List<ProjectModelLintel> Lintels { get; set; } = new();
    public List<ProjectModelWarning> Warnings { get; set; } = new();
    public ProjectModelExtractionMetadata ExtractionMetadata { get; set; } = new();
    public List<string> MissingData { get; set; } = new();
    public List<ProjectModelConflict> Conflicts { get; set; } = new();
}

public sealed class ProjectModelSlab
{
    public string? CoverageDescription { get; set; }
    public double? ThicknessCm { get; set; }
    public string? Concrete { get; set; }
    public double? SteelBottomKg { get; set; }
    public double? SteelTopKg { get; set; }
    public double? SteelDiameterMm { get; set; }
    public double? AreaM2 { get; set; }
}

public sealed class ProjectModelElevation
{
    public string Orientation { get; set; } = string.Empty;
    public string? SourceDrawing { get; set; }
    public List<ProjectModelElevationFinish> Finishes { get; set; } = new();
    public List<ProjectModelElevationOpening> Openings { get; set; } = new();
}

public sealed class ProjectModelElevationFinish
{
    public string Zone { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class ProjectModelElevationOpening
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? Location { get; set; }
}

public sealed class ProjectModelWarning
{
    public string? Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? SourceGroup { get; set; }
}

public sealed class ProjectModelExtractionMetadata
{
    public string? PipelineVersion { get; set; }
    public List<string> ThematicGroups { get; set; } = new();
    public int? TokenUsage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class ProjectModelMetadata
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? Investor { get; set; }
    public string? Author { get; set; }
    public string? Collaborator { get; set; }
    public string? Date { get; set; }
    public string? Phase { get; set; }
}

public sealed class ProjectModelSite
{
    public double? PlotAreaM2 { get; set; }
    public double? BuildingFootprintM2 { get; set; }
    public double? BuildingVolumeM3 { get; set; }
}

public sealed class ProjectModelFloor
{
    public string Level { get; set; } = string.Empty;
    public int Order { get; set; }
    public double? TotalAreaM2 { get; set; }
    public string? AreaNotes { get; set; }
    public List<ProjectModelRoom> Rooms { get; set; } = new();
}

public sealed class ProjectModelRoom
{
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public double? WidthM { get; set; }
    public double? LengthM { get; set; }
    public double? HeightM { get; set; }
    public double? AreaM2 { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProjectModelWalls
{
    public ProjectModelWallGroup External { get; set; } = new();
    public ProjectModelWallGroup InternalLoadBearing { get; set; } = new();
    public ProjectModelWallGroup Partition { get; set; } = new();
}

public sealed class ProjectModelWallGroup
{
    public double? ThicknessCm { get; set; }
    public List<ProjectModelWallLayer> Layers { get; set; } = new();
}

public sealed class ProjectModelWallLayer
{
    public string Material { get; set; } = string.Empty;
    public double? ThicknessCm { get; set; }
}

public sealed class ProjectModelFoundations
{
    public string? Concrete { get; set; }
    public List<ProjectModelFooting> Footings { get; set; } = new();
    public List<ProjectModelPad> Pads { get; set; } = new();
    public string? FoundationWall { get; set; }
}

public sealed class ProjectModelFooting
{
    public string? Symbol { get; set; }
    public double? WidthM { get; set; }
    public double? HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? Reinforcement { get; set; }
    public List<ProjectModelFootingSegment> Segments { get; set; } = new();
}

public sealed class ProjectModelFootingSegment
{
    public string? Id { get; set; }

    public double? LengthM { get; set; }
}

public sealed class ProjectModelPad
{
    public string? Symbol { get; set; }
    public double? BM { get; set; }
    public double? LM { get; set; }
    public double? HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? Reinforcement { get; set; }
}

public sealed class ProjectModelCeiling
{
    public string? CoverageDescription { get; set; }
    public double? ThicknessCm { get; set; }
    public string? Concrete { get; set; }
    public double? SteelBottomKg { get; set; }
    public double? SteelTopKg { get; set; }
    public double? SteelDiameterMm { get; set; }
}

public sealed class ProjectModelRoof
{
    public double? PitchDegrees { get; set; }
    public double? AreaM2 { get; set; }
    public string? WoodClass { get; set; }
    public List<ProjectModelTimberGroup> TimberGroups { get; set; } = new();
    public double? TotalTimberVolumeM3 { get; set; }
    public string? CoveringType { get; set; }
}

public sealed class ProjectModelTimberGroup
{
    public string Element { get; set; } = string.Empty;
    public string? Section { get; set; }
    public int? Count { get; set; }
    public double? LengthM { get; set; }
    public double? VolumeM3 { get; set; }
}

public sealed class ProjectModelColumn
{
    public string Symbol { get; set; } = string.Empty;
    public double? BCm { get; set; }
    public double? HCm { get; set; }
    public double? HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? LongitudinalBars { get; set; }
    public string? Stirrups { get; set; }
}

public sealed class ProjectModelBeam
{
    public string Symbol { get; set; } = string.Empty;
    public double? SpanM { get; set; }
    public double? BwCm { get; set; }
    public double? HCm { get; set; }
    public string? ConcreteClass { get; set; }
    public string? MainBars { get; set; }
}

public sealed class ProjectModelLintel
{
    public string Symbol { get; set; } = string.Empty;
    public double? SpanM { get; set; }
    public double? BwCm { get; set; }
    public double? HCm { get; set; }
    public string? ConcreteClass { get; set; }
    public string? MainBars { get; set; }
    public string? Stirrups { get; set; }
}

public sealed class ProjectModelConflict
{
    public string FieldPath { get; set; } = string.Empty;
    public string? ValueA { get; set; }
    public string? ValueB { get; set; }
    public bool Conflict { get; set; } = true;
}
