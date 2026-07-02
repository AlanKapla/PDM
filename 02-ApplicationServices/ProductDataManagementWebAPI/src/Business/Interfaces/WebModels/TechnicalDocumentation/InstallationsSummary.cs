namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class InstallationsSummary
{
    public InstallationVentilationSummary? Ventilation { get; set; }
    public InstallationPlumbingSummary? Plumbing { get; set; }
    public InstallationElectricalSummary? Electrical { get; set; }
    public InstallationHeatingSummary? Heating { get; set; }
}

public sealed class InstallationVentilationSummary
{
    public string? Type { get; set; }
    public string? Notes { get; set; }
    public List<string> SourceDrawings { get; set; } = new();
}

public sealed class InstallationPlumbingSummary
{
    public List<string> Floors { get; set; } = new();
    public InstallationSewageSummary? Sewage { get; set; }
    public InstallationWaterSupplySummary? WaterSupply { get; set; }
}

public sealed class InstallationSewageSummary
{
    public string? Type { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class InstallationWaterSupplySummary
{
    public string? Type { get; set; }
    public string? Notes { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class InstallationElectricalSummary
{
    public string? Type { get; set; }
    public string? Notes { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class InstallationHeatingSummary
{
    public string? Type { get; set; }
    public string? RoomNumber { get; set; }
    public double? AreaM2 { get; set; }
    public string? Notes { get; set; }
}
