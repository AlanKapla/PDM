namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class MaterialItem
{
    public string Element { get; set; } = string.Empty;
    public string Calculation { get; set; } = string.Empty;
    public List<string> SourceDrawings { get; set; } = new();
    public double NetQuantity { get; set; }
    public double WastePercent { get; set; }
    public double GrossQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? Specification { get; set; }
    public string? MissingData { get; set; }
}
