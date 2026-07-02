namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class ExtractionFocusRoute
{
    public string FocusA { get; set; } = string.Empty;
    public string FocusB { get; set; } = string.Empty;
    public bool RequiresCrossValidation { get; set; }
}
