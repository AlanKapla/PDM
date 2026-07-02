namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class JoinerySummary
{
    public List<string> SourceDrawings { get; set; } = new();
    public string? Notes { get; set; }
    public JoineryExteriorSummary? Exterior { get; set; }
    public JoineryInteriorSummary? Interior { get; set; }
}

public sealed class JoineryExteriorSummary
{
    public List<JoineryDoorEntry> Doors { get; set; } = new();
    public List<JoineryWindowEntry> Windows { get; set; } = new();
}

public sealed class JoineryInteriorSummary
{
    public List<JoineryInteriorDoorEntry> Doors { get; set; } = new();
}

public sealed class JoineryDoorEntry
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? Location { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class JoineryWindowEntry
{
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Count { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class JoineryInteriorDoorEntry
{
    public string Type { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public int CountEstimated { get; set; }
}
