namespace Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

public sealed class ElevationDrawingData
{
    public string? Title { get; set; }
    public List<ElevationFinish> Finishes { get; set; } = new();
    public ElevationLevels? Levels { get; set; }
}

public sealed class ElevationFinish
{
    public string Zone { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class ElevationLevels
{
    public double? GroundFloor { get; set; }
    public double? WindowTop { get; set; }
    public double? Ridge { get; set; }
}
