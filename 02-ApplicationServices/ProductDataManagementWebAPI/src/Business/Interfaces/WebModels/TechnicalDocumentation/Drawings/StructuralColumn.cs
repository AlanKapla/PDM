namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class StructuralColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = string.Empty;
    public double? BCm { get; set; }
    public double? HCm { get; set; }
    public double? HeightM { get; set; }
    public string? ConcreteClass { get; set; }
    public string? LongitudinalBars { get; set; }
    public string? Stirrups { get; set; }
}
