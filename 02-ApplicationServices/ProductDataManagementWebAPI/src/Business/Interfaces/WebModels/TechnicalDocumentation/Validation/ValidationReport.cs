namespace Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

public sealed class ValidationReport
{
    public int TotalFields { get; set; }
    public int HighConfidence { get; set; }
    public int MediumConfidence { get; set; }
    public int LowConfidence { get; set; }
    public List<FieldDisagreement> Disagreements { get; set; } = new();
}
