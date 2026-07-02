namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class SlabDetail
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public double AreaM2 { get; set; }
    public double ThicknessCm { get; set; }
    public string? ConcreteClass { get; set; }
    public string? Reinforcement { get; set; }
}
