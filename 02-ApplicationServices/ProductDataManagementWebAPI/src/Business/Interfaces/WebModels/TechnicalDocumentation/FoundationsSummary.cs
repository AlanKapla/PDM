namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class FoundationsSummary
{
    public List<string> SourceDrawings { get; set; } = new();
    public string? ConcreteClass { get; set; }
    public string? SteelSpecification { get; set; }
    public int? CoverageMm { get; set; }
    public double? FoundationLevelM { get; set; }
    public double? FoundationBottomLevelM { get; set; }
    public List<FoundationFootingSummary> Footings { get; set; } = new();
    public double? TotalFootingLengthM { get; set; }
    public List<FoundationPadSummary> Pads { get; set; } = new();
    public FoundationWallSummary? FoundationWall { get; set; }
    public List<FoundationConnectionDetailSummary> ConnectionDetails { get; set; } = new();
}

public sealed class FoundationFootingSummary
{
    public string? Symbol { get; set; }
    public double? WidthM { get; set; }
    public double? HeightM { get; set; }
    public List<FoundationFootingSegmentSummary> Segments { get; set; } = new();
    public double? TotalLengthM { get; set; }
}

public sealed class FoundationFootingSegmentSummary
{
    public string? Id { get; set; }
    public double LengthM { get; set; }
}

public sealed class FoundationPadSummary
{
    public string? Symbol { get; set; }
    public double? BM { get; set; }
    public double? LM { get; set; }
    public double? HeightM { get; set; }
    public int Count { get; set; } = 1;
    public string? SourceDrawing { get; set; }
}

public sealed class FoundationWallSummary
{
    public string? Material { get; set; }
    public double? ThicknessCm { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class FoundationConnectionDetailSummary
{
    public string Title { get; set; } = string.Empty;
    public string? Reinforcement { get; set; }
    public string? SourceDrawing { get; set; }
}
