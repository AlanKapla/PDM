namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class FoundationMaterials
{
    public List<MaterialItem> Concrete { get; set; } = new();
    public List<MaterialItem> Steel { get; set; } = new();
    public List<MaterialItem> Blocks { get; set; } = new();
}
