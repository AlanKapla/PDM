namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class DrawingDependencyLink
{
    public string SourceFileName { get; set; } = string.Empty;
    public int SourcePageNumber { get; set; }
    public string? SourceSheetNumber { get; set; }
    public string ReferenceLabel { get; set; } = string.Empty;
    public string DetailType { get; set; } = string.Empty;
    public string? TargetFileName { get; set; }
    public int? TargetPageNumber { get; set; }
    public string? TargetSheetNumber { get; set; }
    public string? TargetTitle { get; set; }
    public string? Notes { get; set; }
}
