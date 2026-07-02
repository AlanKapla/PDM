namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class OpeningScheduleItem
{
    public string Type { get; set; } = string.Empty;
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public int Count { get; set; }
    public string? Material { get; set; }
}
