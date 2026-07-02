namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class Room
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string? Symbol { get; set; }
    public double WidthM { get; set; }
    public double LengthM { get; set; }
    public double? HeightM { get; set; }
    public double AreaM2 { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}
