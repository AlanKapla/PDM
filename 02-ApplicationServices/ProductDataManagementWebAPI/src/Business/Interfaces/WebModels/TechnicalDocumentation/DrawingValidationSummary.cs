namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class DrawingValidationSummary
{
    public string FileName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string? SheetNumber { get; set; }
    public string DrawingType { get; set; } = string.Empty;
    public bool CrossValidationUsed { get; set; }
    public string ConfidenceScore { get; set; } = "high";
    public List<string> Disagreements { get; set; } = new();
}
