using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class FoundationSection
{
    public string? ConcreteClass { get; set; }
    public string? SteelSpecification { get; set; }
    public int? CoverageMm { get; set; }
    public double? FoundationLevelM { get; set; }
    public List<FootingDetail> Footings { get; set; } = new();
    public List<PadDetail> Pads { get; set; } = new();
    public FoundationWallDetail? FoundationWall { get; set; }
    public List<MaterialQuantity> Blocks { get; set; } = new();
    public List<MaterialQuantity> Concrete { get; set; } = new();
    public List<MaterialQuantity> Steel { get; set; } = new();
    public List<MaterialQuantity> Insulation { get; set; } = new();
}
