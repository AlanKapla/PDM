namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class SectionDrawingData
{
    public SectionLevels? Levels { get; set; }
    public List<SectionZone> FloorZones { get; set; } = new();
    public List<SectionZone> RoofZones { get; set; } = new();
    public SectionBeamDetail? RingBeam { get; set; }
    public List<SectionRingBeamDetail> RingBeams { get; set; } = new();
    public SectionCollarWallDetail? CollarWall { get; set; }
    public SectionBeamDetail? Purlin { get; set; }
    public SectionBeamDetail? WallPlate { get; set; }
    public List<ThermalInsulationElementDetail> ThermalInsulation { get; set; } = new();
}

public sealed class SectionLevels
{
    public double? FoundationBottomM { get; set; }
    public double? GroundFloorM { get; set; }
    public double? CeilingM { get; set; }
    public double? RidgeM { get; set; }
}

public sealed class SectionZone
{
    public string Zone { get; set; } = string.Empty;
    public string? SourceDrawing { get; set; }
    public List<WallLayer> Layers { get; set; } = new();
}

public sealed class SectionBeamDetail
{
    public string? Location { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? Reinforcement { get; set; }
}

public sealed class SectionRingBeamDetail
{
    public string? Location { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? Reinforcement { get; set; }
}

public sealed class SectionCollarWallDetail
{
    public double? ThicknessCm { get; set; }
    public double? HeightCm { get; set; }
    public SectionCollarWallTimberDetail? Timber { get; set; }
    public SectionRingBeamDetail? RingBeam { get; set; }
}

public sealed class SectionCollarWallTimberDetail
{
    public string? Section { get; set; }
    public string? Material { get; set; }
}

public sealed class ThermalInsulationElementDetail
{
    public string Element { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public double? ThicknessCm { get; set; }
    public string? System { get; set; }
    public string? Notes { get; set; }
}
