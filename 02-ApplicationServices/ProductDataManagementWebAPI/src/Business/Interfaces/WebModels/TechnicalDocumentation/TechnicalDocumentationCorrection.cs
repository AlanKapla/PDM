namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class TechnicalDocumentationCorrection
{
    public string FieldPath { get; set; } = string.Empty;
    public string? CorrectedBy { get; set; }
    public DateTimeOffset? CorrectedAt { get; set; }
    public string? Reason { get; set; }
}
