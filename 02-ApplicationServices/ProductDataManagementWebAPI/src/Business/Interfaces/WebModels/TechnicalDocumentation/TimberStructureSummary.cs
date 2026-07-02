namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class TimberStructureSummary
{
    public string? WoodClass { get; set; }
    public string? SourceDrawing { get; set; }
    public double? TotalVolumeM3 { get; set; }
    public string? Notes { get; set; }
    public List<TimberGroupSummary> Groups { get; set; } = new();
}

public sealed class TimberGroupSummary
{
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }
    public List<TimberStructureRow> Rows { get; set; } = new();
    public double? GroupSumMb { get; set; }
    public double? GroupVolumeM3 { get; set; }
}

public sealed class TimberStructureRow
{
    public int Count { get; set; }
    public double LengthM { get; set; }
    public double RowSumMb { get; set; }
}
