namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class Wall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public double LengthM { get; set; }
    public double ThicknessCm { get; set; }
    public double? GrossAreaM2 { get; set; }
    public double? NetAreaM2 { get; set; }
    public List<WallLayer> Layers { get; set; } = new();
}
