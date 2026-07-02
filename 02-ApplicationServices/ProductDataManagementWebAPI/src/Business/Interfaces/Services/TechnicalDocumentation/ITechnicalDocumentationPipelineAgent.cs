using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Interfaces.Services.TechnicalDocumentation;

public interface ITechnicalDocumentationPipelineAgent
{
    string Name { get; }

    Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken);
}

public static class TechnicalDocumentationPipelineAgentNames
{
    public const string Ingestion = "Ingestion";
    public const string Classification = "Classification";
    public const string Grouping = "Grouping";
    public const string GroupExtraction = "GroupExtraction";
    public const string Verification = "Verification";
    public const string Consolidation = "Consolidation";
    public const string Audit = "Audit";
    public const string Output = "Output";
    public const string ImageExtraction = "ImageExtraction";
    public const string CrossReference = "CrossReference";
    public const string Rooms = "Rooms";
    public const string Openings = "Openings";
    public const string MaterialsCalculation = "MaterialsCalculation";
    public const string Report = "Report";
    public const string DetailsValidation = "DetailsValidation";
}

public sealed class TechnicalDocumentationAgentContext
{
    public List<TechnicalDocumentationImageInput> Images { get; init; } = new();

    public List<TechnicalDocumentationImageInput> PreparedImages { get; } = new();

    public List<ClassifiedTechnicalDocumentationImage> ClassifiedImages { get; } = new();

    public List<DrawingClassification> Classifications { get; } = new();

    public List<ThematicDrawingGroup> ThematicGroups { get; } = new();

    public List<GroupExtractionPairResult> GroupExtractionResults { get; } = new();

    public List<VerifiedGroupExtractionResult> VerifiedGroupExtractions { get; } = new();

    public List<string> PipelineWarnings { get; } = new();

    public List<FloorPlanDrawing> Drawings { get; } = new();

    public List<DrawingDependencyLink> Dependencies { get; } = new();

    public ProjectTechnicalDocumentationDetails Details { get; } = new();

    public Dictionary<string, object> SharedState { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<TechnicalDocumentationPartialResult> PartialResults { get; } = new();

    public List<TechnicalDocumentationAgentResult> AgentExecutions { get; } = new();

    public List<string> FailedPages { get; } = new();

    public ProjectModel? ProjectModel { get; set; }

    public MaterialSchedule? ComputedMaterialSchedule { get; set; }
}

public sealed record TechnicalDocumentationAgentResult(
    bool Success,
    string AgentName,
    string Summary,
    List<string> Warnings,
    Exception? Error = null,
    IReadOnlyList<string>? ContributedFields = null);

public interface ITechnicalDocumentationPipelineRunner
{
    Task<ProjectTechnicalDocumentationDetails> RunAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken);
}
