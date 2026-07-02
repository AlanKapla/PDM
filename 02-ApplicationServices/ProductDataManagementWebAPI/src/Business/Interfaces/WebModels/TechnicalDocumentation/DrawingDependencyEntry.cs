namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class DrawingDependencyEntry
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
}
