using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public enum MaterialDrawingGroupKind
{
    Foundations,
    Walls,
    Ceilings,
    Roof
}

public sealed class MaterialDrawingGroup
{
    public MaterialDrawingGroupKind Kind { get; init; }

    public string Label { get; init; } = string.Empty;

    public IReadOnlyList<FloorPlanDrawing> Drawings { get; init; } = [];
}
