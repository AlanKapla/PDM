namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class WallsSummary
{
    public List<string> SourceDrawings { get; set; } = new();
    public WallExternalSummary? External { get; set; }
    public WallInternalSummary? Internal { get; set; }
    public CollarWallSummary? CollarWall { get; set; }
    public List<RingBeamSummary> RingBeams { get; set; } = new();
    public List<WallColumnSummary> Columns { get; set; } = new();
}

public sealed class WallExternalSummary
{
    public double? ThicknessCm { get; set; }
    public List<WallLayerSummary> Layers { get; set; } = new();
    public List<WallFinishSummary> Finishes { get; set; } = new();
}

public sealed class WallLayerSummary
{
    public string Material { get; set; } = string.Empty;
    public double? ThicknessCm { get; set; }
}

public sealed class WallFinishSummary
{
    public string Zone { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string? Color { get; set; }
    public List<string> SourceDrawings { get; set; } = new();
}

public sealed class WallInternalSummary
{
    public WallInternalGroupSummary? LoadBearing { get; set; }
    public WallInternalGroupSummary? Partition { get; set; }
}

public sealed class WallInternalGroupSummary
{
    public double? ThicknessCm { get; set; }
    public string? Material { get; set; }
}

public sealed class CollarWallSummary
{
    public double? ThicknessCm { get; set; }
    public double? HeightCm { get; set; }
    public CollarWallTimberSummary? Timber { get; set; }
    public RingBeamSummary? RingBeam { get; set; }
}

public sealed class CollarWallTimberSummary
{
    public string? Section { get; set; }
    public string? Material { get; set; }
}

public sealed class RingBeamSummary
{
    public string? Location { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? Reinforcement { get; set; }
}

public sealed class WallColumnSummary
{
    public string Symbol { get; set; } = string.Empty;
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? Reinforcement { get; set; }
    public string? SourceDrawing { get; set; }
}
