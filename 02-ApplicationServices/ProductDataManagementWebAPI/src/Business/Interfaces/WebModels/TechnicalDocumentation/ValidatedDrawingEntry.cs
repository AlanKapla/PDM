namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class ValidatedDrawingEntry
{
    public string? SheetNumber { get; set; }
    public string DrawingType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? Scale { get; set; }
    public bool Validated { get; set; } = true;
    public bool HasMaterialTable { get; set; }
}
