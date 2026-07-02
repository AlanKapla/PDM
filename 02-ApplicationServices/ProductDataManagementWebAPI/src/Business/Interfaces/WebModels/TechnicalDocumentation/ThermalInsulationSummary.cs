namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class ThermalInsulationSummary
{
    public List<string> SourceDrawings { get; set; } = new();
    public List<ThermalInsulationElement> Elements { get; set; } = new();
}

public sealed class ThermalInsulationElement
{
    public string Element { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public double? ThicknessCm { get; set; }
    public string? System { get; set; }
    public string? Notes { get; set; }
}
