namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class RelatedDrawingRef
{
    public string ReferenceLabel { get; set; } = string.Empty;
    public string? TargetSheetNumber { get; set; }
    public string? TargetTitle { get; set; }
    public string DetailType { get; set; } = string.Empty;
}
