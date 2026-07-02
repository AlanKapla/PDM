namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class RoofSummary
{
    public double? PitchDegrees { get; set; }
    public double? AreaM2 { get; set; }
    public string? CoveringType { get; set; }
    public string? SourceDrawing { get; set; }
    public List<RoofWindowEntry> RoofWindows { get; set; } = new();
    public RoofVentilationEntry? Ventilation { get; set; }
    public RoofDrainageEntry? Drainage { get; set; }
    public List<RoofLayerZone> Layers { get; set; } = new();
}

public sealed class RoofWindowEntry
{
    public string Type { get; set; } = string.Empty;
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public int Count { get; set; }
    public string? Location { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class RoofVentilationEntry
{
    public string? Type { get; set; }
    public int Count { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class RoofDrainageEntry
{
    public int? DownpipeDiameterMm { get; set; }
    public double? MinSlopePct { get; set; }
    public string? Notes { get; set; }
}

public sealed class RoofLayerZone
{
    public string Zone { get; set; } = string.Empty;
    public List<string> Sequence { get; set; } = new();
}
