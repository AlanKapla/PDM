namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class DrawingCrossReference
{
    public string ReferenceLabel { get; set; } = string.Empty;
    public string? TargetSheetNumber { get; set; }
    public string? TargetTitle { get; set; }
    public string DetailType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ResolvedFileName { get; set; }
    public int? ResolvedPageNumber { get; set; }
}
