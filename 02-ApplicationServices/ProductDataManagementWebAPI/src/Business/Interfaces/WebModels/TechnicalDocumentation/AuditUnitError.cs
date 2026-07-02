namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class AuditUnitError
{
    public string Field { get; set; } = string.Empty;
    public string Found { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
}
