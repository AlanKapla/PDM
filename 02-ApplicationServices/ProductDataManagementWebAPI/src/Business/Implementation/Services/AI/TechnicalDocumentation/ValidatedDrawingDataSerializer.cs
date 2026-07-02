using System.Text.Json;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ValidatedDrawingDataSerializer
{
    public static string Serialize(
        IReadOnlyList<FloorPlanDrawing> drawings,
        JsonSerializerOptions options)
    {
        List<ValidatedDrawingPayload> payload = drawings
            .Select(drawing => new ValidatedDrawingPayload
            {
                Source = drawing.Source,
                Classification = drawing.Classification,
                TotalAreaM2 = drawing.TotalAreaM2,
                ExternalDimensions = drawing.ExternalDimensions,
                Rooms = drawing.Rooms,
                Walls = drawing.Walls,
                Openings = drawing.Openings,
                Columns = drawing.Columns,
                Beams = drawing.Beams,
                Lintels = drawing.Lintels,
                Foundations = drawing.Foundations,
                Floors = drawing.Floors,
                Roof = drawing.Roof,
                Site = drawing.Site,
                Section = drawing.Section,
                Elevation = drawing.Elevation,
                Details = drawing.Details,
                Installations = drawing.Installations,
                ValidationReport = drawing.ValidationReport
            })
            .ToList();

        return JsonSerializer.Serialize(payload, options);
    }

    private sealed class ValidatedDrawingPayload
    {
        public DrawingSource Source { get; set; } = new();
        public DrawingClassification Classification { get; set; } = new();
        public double? TotalAreaM2 { get; set; }
        public DrawingExternalDimensions? ExternalDimensions { get; set; }
        public List<Room> Rooms { get; set; } = new();
        public List<Wall> Walls { get; set; } = new();
        public List<Opening> Openings { get; set; } = new();
        public List<StructuralColumn> Columns { get; set; } = new();
        public List<StructuralBeam> Beams { get; set; } = new();
        public List<StructuralLintel> Lintels { get; set; } = new();
        public FoundationSection? Foundations { get; set; }
        public FloorSection? Floors { get; set; }
        public RoofSection? Roof { get; set; }
        public SitePlanSection? Site { get; set; }
        public SectionDrawingData? Section { get; set; }
        public ElevationDrawingData? Elevation { get; set; }
        public List<StructuralDetail> Details { get; set; } = new();
        public List<DrawingInstallation> Installations { get; set; } = new();
        public Business.Interfaces.WebModels.TechnicalDocumentation.Validation.ValidationReport? ValidationReport { get; set; }
    }
}
