namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class WallMaterials
{
    public List<MaterialItem> Masonry { get; set; } = new();
    public List<MaterialItem> Mortar { get; set; } = new();
    public List<MaterialItem> Insulation { get; set; } = new();
}
