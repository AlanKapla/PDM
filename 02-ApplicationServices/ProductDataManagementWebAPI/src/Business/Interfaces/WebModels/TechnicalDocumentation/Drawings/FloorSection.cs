using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class FloorSection
{
    public string? CoverageDescription { get; set; }
    public string? BasicGrid { get; set; }
    public string? Notes { get; set; }
    public double? TotalMassKg { get; set; }
    public List<SlabDetail> Slabs { get; set; } = new();
    public List<RebarBar> Bars { get; set; } = new();
    public List<MaterialQuantity> Concrete { get; set; } = new();
    public List<MaterialQuantity> Steel { get; set; } = new();
}
