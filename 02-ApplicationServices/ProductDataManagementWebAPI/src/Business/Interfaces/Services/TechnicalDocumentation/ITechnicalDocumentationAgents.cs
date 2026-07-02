using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Interfaces.Services.TechnicalDocumentation;

public interface IDrawingClassificationAgent
{
    Task<DrawingClassification> ClassifyAsync(
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken);
}

public interface IExtractionFocusRouter
{
    ExtractionFocusRoute Resolve(DrawingClassification classification);
}

public interface IArchitecturalExtractionAgent
{
    Task<FloorPlanDrawing> ExtractAsync(
        byte[] imageBytes,
        string mediaType,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext,
        string? focusPrompt,
        CancellationToken cancellationToken);
}

public interface IExtractionAgentB
{
    Task<FloorPlanDrawing> ExtractAsync(
        byte[] imageBytes,
        string mediaType,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext,
        string? focusPrompt,
        CancellationToken cancellationToken);
}

public interface IUniversalExtractionAgent
{
    Task<FloorPlanDrawing> ExtractAsync(
        byte[] imageBytes,
        string mediaType,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext,
        string focusPrompt,
        CancellationToken cancellationToken);
}

public interface IComparatorAgent
{
    Task<FloorPlanDrawing> CompareAsync(
        byte[] imageBytes,
        string mediaType,
        FloorPlanDrawing resultA,
        FloorPlanDrawing resultB,
        DrawingClassification classification,
        CancellationToken cancellationToken);
}

public interface IAggregationAgent
{
    Task<ProjectModel> AggregateAsync(
        IReadOnlyList<FloorPlanDrawing> drawings,
        CancellationToken cancellationToken);
}

public interface IMaterialCalculationAgent
{
    Task<MaterialSchedule> CalculateAsync(
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object>? sharedState = null);
}

public interface IAuditAgent
{
    Task<AuditResult> AuditAsync(
        ProjectModel projectModel,
        MaterialSchedule? materialSchedule,
        CancellationToken cancellationToken);
}

public interface IDetailsValidationAgent
{
    Task<DetailsValidationResult> ValidateAsync(
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<TechnicalDocumentationPartialResult> partialResults,
        CancellationToken cancellationToken);
}

public interface IMaterialOrchestrationService
{
    Task<MaterialSchedule> OrchestrateAsync(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        ProjectTechnicalDocumentationDetails details,
        string buildingType,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object>? sharedState = null);
}

public interface IGroupExtractionAgentService
{
    Task<(string ResultAJson, string ResultBJson)> ExtractGroupAsync(
        ThematicDrawingGroup group,
        CancellationToken cancellationToken);
}

public interface IExtractionVerificationAgentService
{
    Task<string> VerifyCriticalDiffsAsync(
        ThematicDrawingGroup group,
        ExtractionDiffResult diff,
        string resultAJson,
        string resultBJson,
        CancellationToken cancellationToken);
}

public interface IConsolidationAgentService
{
    Task<ProjectModel> ConsolidateAsync(
        IReadOnlyList<VerifiedGroupExtractionResult> verifiedGroups,
        CancellationToken cancellationToken);
}
