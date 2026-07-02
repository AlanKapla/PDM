namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class MaterialQuantity
{
    public string MaterialType { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
