namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class PadDetail
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Symbol { get; set; }
    public int Count { get; set; } = 1;
    public double BM { get; set; }
    public double LM { get; set; }
    public double HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? Reinforcement { get; set; }
}
