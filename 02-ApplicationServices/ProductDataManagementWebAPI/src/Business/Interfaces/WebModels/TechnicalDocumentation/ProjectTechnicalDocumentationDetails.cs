using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Interfaces.WebModels.TechnicalDocumentation;

/// <summary>
/// In-memory working model for the technical documentation pipeline.
/// Serialized to <see cref="TechnicalDocumentationDetailsJsonRoot"/> (§8.1) via
/// <see cref="Business.Implementation.Helpers.TechnicalDocumentationDetailsSerializer"/>.
/// Legacy summary fields remain for backward-compatible reads of old <c>DetailsJson</c>.
/// </summary>
public sealed class ProjectTechnicalDocumentationDetails
{
    [JsonIgnore]
    public ProjectModel? ProjectModel { get; set; }

    public ProjectInfo Project { get; set; } = new();
    public double TotalAreaM2 { get; set; }
    public List<RoomFloorGroup> Rooms { get; set; } = new();
    public RoofSummary? Roof { get; set; }
    public TimberStructureSummary? TimberStructure { get; set; }
    public WallsSummary? Walls { get; set; }
    public FloorsSummary? Floors { get; set; }
    public FoundationsSummary? Foundations { get; set; }
    public ThermalInsulationSummary? ThermalInsulation { get; set; }
    public JoinerySummary? Joinery { get; set; }
    public InstallationsSummary Installations { get; set; } = new();
    public List<ValidatedDrawingEntry> ValidatedDrawings { get; set; } = new();
    public List<DrawingDependencyEntry> DrawingDependencies { get; set; } = new();
    public DetailsMaterialSchedule? MaterialSchedule { get; set; }
    public AuditResult? AuditResult { get; set; }
    public DetailsValidationResult? ValidationReview { get; set; }

    [JsonIgnore]
    public List<DrawingValidationSummary> ValidationSummaries { get; set; } = new();

    [JsonIgnore]
    public List<TechnicalDocumentationCorrection> Corrections { get; set; } = new();

    public int TokenUsage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
