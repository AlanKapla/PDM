namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class DetailsMaterialSchedule
{
    public DateTime? CalculatedAt { get; set; }
    public DetailsMaterialScheduleGroups Groups { get; set; } = new();
    public DetailsMaterialScheduleTotals? Totals { get; set; }
}

public sealed class DetailsMaterialScheduleGroups
{
    public DetailsMaterialScheduleFoundationGroup? Foundations { get; set; }
    public DetailsMaterialScheduleSlabGroup? Slabs { get; set; }
    public DetailsMaterialScheduleRoofGroup? Roof { get; set; }
    public DetailsMaterialScheduleSiteGroup? Site { get; set; }
}

public sealed class DetailsMaterialScheduleFoundationGroup
{
    public List<DetailsMaterialScheduleItem> Concrete { get; set; } = new();
    public List<DetailsMaterialScheduleItem> Steel { get; set; } = new();
    public List<DetailsMaterialScheduleItem> Masonry { get; set; } = new();
    public List<DetailsMaterialScheduleItem> Insulation { get; set; } = new();
}

public sealed class DetailsMaterialScheduleSlabGroup
{
    public List<DetailsMaterialScheduleItem> Concrete { get; set; } = new();
    public List<DetailsMaterialScheduleItem> Steel { get; set; } = new();
}

public sealed class DetailsMaterialScheduleRoofGroup
{
    public List<DetailsMaterialScheduleItem> Timber { get; set; } = new();
    public List<DetailsMaterialScheduleItem> Covering { get; set; } = new();
}

public sealed class DetailsMaterialScheduleSiteGroup
{
    public double? PlotAreaM2 { get; set; }
    public double? BuildingFootprintM2 { get; set; }
    public double? PavedAreaM2 { get; set; }
    public double? GreenAreaM2 { get; set; }
    public double? BuildingCoverageRatio { get; set; }
    public double? CubatureM3 { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class DetailsMaterialScheduleItem
{
    public string Element { get; set; } = string.Empty;
    public double? NetM3 { get; set; }
    public double? GrossM3 { get; set; }
    public double? NetM2 { get; set; }
    public double? GrossM2 { get; set; }
    public double? NetKg { get; set; }
    public double? GrossKg { get; set; }
    public string? Unit { get; set; }
    public double? WastePercent { get; set; }
    public string? SourceType { get; set; }
    public string? SourceDrawing { get; set; }
}

public sealed class DetailsMaterialScheduleTotals
{
    public double? ConcreteM3 { get; set; }
    public double? SteelKg { get; set; }
    public double? TimberM3 { get; set; }
    public double? InsulationM2 { get; set; }
}
