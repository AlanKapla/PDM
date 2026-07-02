using System.Text;
using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class MaterialOrchestrationService : IMaterialOrchestrationService
{
    private const string AuditAgentName = "material-orchestration-agent";

    private static readonly JsonSerializerOptions CompactJsonOptions = TechnicalDocumentationJsonHelper.CreateCompactSerializerOptions();

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly ILogger<MaterialOrchestrationService> logger;

    public MaterialOrchestrationService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        ILogger<MaterialOrchestrationService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.logger = logger;
    }

    public async Task<MaterialSchedule> OrchestrateAsync(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        ProjectTechnicalDocumentationDetails details,
        string buildingType,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object>? sharedState = null)
    {
        ConsolidatedProjectMaterials consolidated = DrawingMaterialConsolidator.Consolidate(drawings, dependencies);
        MaterialSchedule schedule = MaterialScheduleBuilder.Build(consolidated, details, drawings, buildingType, sharedState);
        schedule = MaterialWasteApplier.Apply(schedule);
        schedule.Summary = MaterialScheduleBuilder.BuildSummaryPublic(schedule);

        try
        {
            List<string> auditWarnings = await RunAuditAgentAsync(
                schedule,
                drawings,
                dependencies,
                buildingType,
                cancellationToken);
            schedule.Warnings.AddRange(auditWarnings);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Material orchestration audit agent failed — using consolidated schedule only");
            schedule.Warnings.Add("Audyt AI harmonogramu materiałów niedostępny — użyto konsolidacji deterministycznej.");
        }

        return MaterialQuantityFilter.PruneZeroQuantities(schedule);
    }

    private async Task<List<string>> RunAuditAgentAsync(
        MaterialSchedule schedule,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        CancellationToken cancellationToken)
    {
        List<MaterialOrchestrationCatalogEntry> catalog = drawings
            .Select(drawing => new MaterialOrchestrationCatalogEntry
            {
                File = drawing.Source.FileName,
                Page = drawing.Source.PageNumber,
                Sheet = drawing.Classification.SheetNumber,
                Type = drawing.Classification.DrawingType,
                Bucket = DrawingViewClassifier.Classify(drawing.Classification).ToString()
            })
            .ToList();

        MaterialOrchestrationAuditRequest request = new()
        {
            BuildingType = buildingType,
            Catalog = catalog,
            Dependencies = dependencies,
            Schedule = schedule
        };

        StringBuilder userPrompt = new();
        userPrompt.Append("audyt:");
        userPrompt.Append(JsonSerializer.Serialize(request, CompactJsonOptions));

        string response = await TechnicalDocumentationAgentInvoker.CompleteAsync(
            completionService,
            agentDefinitionLoader,
            AuditAgentName,
            userPrompt.ToString(),
            cancellationToken);

        MaterialOrchestrationAuditResponse? audit = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            response,
            CompactJsonOptions,
            new MaterialOrchestrationAuditResponse(),
            logger,
            "MaterialOrchestrationAudit");

        return audit.Warnings;
    }

    private sealed class MaterialOrchestrationAuditRequest
    {
        public string BuildingType { get; set; } = string.Empty;
        public List<MaterialOrchestrationCatalogEntry> Catalog { get; set; } = new();
        public IReadOnlyList<DrawingDependencyLink> Dependencies { get; set; } = [];
        public MaterialSchedule Schedule { get; set; } = new();
    }

    private sealed class MaterialOrchestrationCatalogEntry
    {
        public string File { get; set; } = string.Empty;
        public int Page { get; set; }
        public string? Sheet { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
    }

    private sealed class MaterialOrchestrationAuditResponse
    {
        public List<string> Warnings { get; set; } = new();
        public List<string> Assumptions { get; set; } = new();
    }
}
