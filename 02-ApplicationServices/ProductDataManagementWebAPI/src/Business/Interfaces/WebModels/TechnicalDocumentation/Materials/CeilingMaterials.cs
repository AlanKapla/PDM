namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class CeilingMaterials
{
    public List<MaterialItem> Concrete { get; set; } = new();
    public List<MaterialItem> Steel { get; set; } = new();
}
