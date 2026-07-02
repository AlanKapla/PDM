namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class AuditResult
{
    public List<string> Warnings { get; set; } = new();
    public List<string> MissingMaterials { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public List<AuditUnitError> UnitErrors { get; set; } = new();
    public List<string> CrossReferenceErrors { get; set; } = new();

    [Obsolete("Use MissingMaterials instead.")]
    public List<string> MissingData
    {
        get => MissingMaterials;
        set => MissingMaterials = value;
    }
}
