namespace Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

public sealed class RoofMaterials
{
    public List<MaterialItem> Covering { get; set; } = new();
    public List<MaterialItem> Timber { get; set; } = new();
    public List<MaterialItem> Insulation { get; set; } = new();
}
