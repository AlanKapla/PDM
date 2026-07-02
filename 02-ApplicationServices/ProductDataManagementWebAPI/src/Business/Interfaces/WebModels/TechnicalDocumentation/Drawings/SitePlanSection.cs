namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class SitePlanSection
{
    public double? PlotAreaM2 { get; set; }
    public double? BuildingFootprintM2 { get; set; }
    public double? PavedAreaM2 { get; set; }
    public double? GreenAreaM2 { get; set; }
    public double? BuildingVolumeM3 { get; set; }
    public double? BuildingCoverageRatio { get; set; }
}
