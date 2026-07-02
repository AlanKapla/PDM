namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class RoofSection
{
    public double AreaM2 { get; set; }
    public double PitchDegrees { get; set; }
    public string CoveringType { get; set; } = string.Empty;
    public string? WoodClass { get; set; }
    public string? Notes { get; set; }
    public double? TotalVolumeM3 { get; set; }
    public RoofVentilationDetail? Ventilation { get; set; }
    public RoofDrainageDetail? Drainage { get; set; }
    public List<TimberGroup> TimberGroups { get; set; } = new();
    public List<TimberElement> Timber { get; set; } = new();
}

public sealed class RoofVentilationDetail
{
    public string? Type { get; set; }
    public int Count { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class RoofDrainageDetail
{
    public int? DownpipeDiameterMm { get; set; }
    public double? MinSlopePct { get; set; }
    public string? Notes { get; set; }
}
