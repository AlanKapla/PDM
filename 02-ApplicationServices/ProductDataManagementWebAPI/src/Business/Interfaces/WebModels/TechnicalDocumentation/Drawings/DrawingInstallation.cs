namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class DrawingInstallation
{
    public string Type { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
    public string? SourceDrawing { get; set; }
    public List<string> SourceDrawings { get; set; } = new();
    public List<string> Floors { get; set; } = new();
    public string? SewageType { get; set; }
    public string? WaterSupplyType { get; set; }
    public string? RoomNumber { get; set; }
    public double? AreaM2 { get; set; }
}
