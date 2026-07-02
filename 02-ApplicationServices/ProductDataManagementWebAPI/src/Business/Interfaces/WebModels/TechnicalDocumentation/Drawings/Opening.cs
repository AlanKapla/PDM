namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class Opening
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public int Count { get; set; }
    public string? Location { get; set; }
    public string? WallId { get; set; }
    public string? Material { get; set; }
    public bool IsInterior { get; set; }
}
