namespace Business.Interfaces.Services.TechnicalDocumentation;

public sealed class ExtractionFieldDiff
{
    public required string FieldPath { get; init; }

    public string? ValueA { get; init; }

    public string? ValueB { get; init; }

    public bool IsCritical { get; init; }
}

public sealed class ExtractionDiffResult
{
    public bool HasCriticalDifferences { get; set; }

    public bool HasMinorDifferences { get; set; }

    public List<ExtractionFieldDiff> Differences { get; } = new();
}
