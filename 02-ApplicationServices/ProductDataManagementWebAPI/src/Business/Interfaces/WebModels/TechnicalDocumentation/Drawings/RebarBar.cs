namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class RebarBar
{
    public int Pos { get; set; }
    public int? Count { get; set; }
    public double? DiameterMm { get; set; }
    public double? LengthM { get; set; }
    public double? TotalLengthM { get; set; }
    public double? MassKg { get; set; }
    public string? Shape { get; set; }
}
