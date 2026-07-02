namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class MaterialSummaryItem
{
    public string Category { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public double GrossQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
