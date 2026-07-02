namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class DeferredDetailNote
{
    public string Topic { get; set; } = string.Empty;
    public string TargetReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
