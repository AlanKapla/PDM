namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class FootingDetail
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Symbol { get; set; }
    public double LengthM { get; set; }
    public double WidthM { get; set; }
    public double HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? Reinforcement { get; set; }

    public List<FootingSegmentDetail> Segments { get; set; } = new();
}
