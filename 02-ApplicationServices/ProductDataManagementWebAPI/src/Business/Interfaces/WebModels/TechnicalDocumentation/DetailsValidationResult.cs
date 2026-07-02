namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class DetailsValidationResult
{
    public List<DetailsValidationDifference> Differences { get; set; } = new();
    public List<string> RootCauses { get; set; } = new();
    public List<DetailsValidationRemediationStep> RemediationSteps { get; set; } = new();
    public List<DetailsValidationImageCheck> ImageChecks { get; set; } = new();
}

public sealed class DetailsValidationDifference
{
    public string Path { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    public string Severity { get; set; } = "medium";
    public List<string> SourceDrawings { get; set; } = new();
}

public sealed class DetailsValidationRemediationStep
{
    public int Order { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? PipelineStage { get; set; }
    public List<string> SourceDrawings { get; set; } = new();
}

public sealed class DetailsValidationImageCheck
{
    public string SheetNumber { get; set; } = string.Empty;
    public string DrawingType { get; set; } = string.Empty;
    public List<string> Findings { get; set; } = new();
    public List<string> ConfirmedDifferences { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}
