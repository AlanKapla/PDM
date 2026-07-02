namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Investor { get; set; }
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? Designer { get; set; }
    public string? Collaborator { get; set; }
    public string? Date { get; set; }
    public string? Phase { get; set; }
    public string? BuildingType { get; set; }
}
