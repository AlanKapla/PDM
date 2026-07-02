namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class InteriorDoorEntry
{
    public string Type { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public int CountEstimated { get; set; }
}
