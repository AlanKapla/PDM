namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class StructuralLintel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = string.Empty;
    public double? SpanM { get; set; }
    public double? BwCm { get; set; }
    public double? HCm { get; set; }
    public double? BCm { get; set; }
    public string? ConcreteClass { get; set; }
    public string? MainBars { get; set; }
    public string? Stirrups { get; set; }
}
