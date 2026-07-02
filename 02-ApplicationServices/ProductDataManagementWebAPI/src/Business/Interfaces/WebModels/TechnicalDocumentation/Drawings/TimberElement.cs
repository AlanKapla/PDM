namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class TimberElement
{
    public string Element { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public double LengthM { get; set; }
    public int Count { get; set; }
    public double? RowSumMb { get; set; }
    public double? VolumeM3 { get; set; }
    public string? WoodType { get; set; }
}
