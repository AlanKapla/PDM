namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class FloorsSummary
{
    public List<string> SourceDrawings { get; set; } = new();
    public double? SlabThicknessCm { get; set; }
    public string? ConcreteClass { get; set; }
    public FloorReinforcementSummary? Reinforcement { get; set; }
    public List<FloorZoneSummary> Zones { get; set; } = new();
}

public sealed class FloorReinforcementSummary
{
    public FloorReinforcementLayerSummary? Bottom { get; set; }
    public FloorReinforcementLayerSummary? Top { get; set; }
}

public sealed class FloorReinforcementLayerSummary
{
    public string? SourceDrawing { get; set; }
    public double? TotalMassKg { get; set; }
    public string? BasicGrid { get; set; }
    public string? Notes { get; set; }
    public List<RebarBarSummary> Bars { get; set; } = new();
}

public sealed class RebarBarSummary
{
    public int Pos { get; set; }
    public int Count { get; set; }
    public int DiameterMm { get; set; }
    public double LengthM { get; set; }
    public double TotalLengthM { get; set; }
    public double MassKg { get; set; }
}

public sealed class FloorZoneSummary
{
    public string Zone { get; set; } = string.Empty;
    public string? SourceDrawing { get; set; }
    public List<FloorLayerSummary> Layers { get; set; } = new();
}

public sealed class FloorLayerSummary
{
    public string Material { get; set; } = string.Empty;
    public double? ThicknessCm { get; set; }
}
