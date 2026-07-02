namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class TimberGroup
{
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public List<TimberGroupRow> Rows { get; set; } = new();
    public double? GroupSumMb { get; set; }
    public double? GroupVolumeM3 { get; set; }
}

public sealed class TimberGroupRow
{
    public int Count { get; set; }
    public double LengthM { get; set; }
    public double? RowSumMb { get; set; }
}
