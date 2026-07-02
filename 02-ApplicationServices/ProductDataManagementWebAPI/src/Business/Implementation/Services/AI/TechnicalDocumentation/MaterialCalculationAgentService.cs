using System.Text;
using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class MaterialCalculationAgentService : IMaterialCalculationAgent
{
    private const string AgentName = "material-calculation-agent";

    private static readonly JsonSerializerOptions CompactJsonOptions = TechnicalDocumentationJsonHelper.CreateCompactSerializerOptions();

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly IMaterialOrchestrationService materialOrchestrationFallback;
    private readonly ILogger<MaterialCalculationAgentService> logger;

    public MaterialCalculationAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        IMaterialOrchestrationService materialOrchestrationFallback,
        ILogger<MaterialCalculationAgentService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.materialOrchestrationFallback = materialOrchestrationFallback;
        this.logger = logger;
    }

    public async Task<MaterialSchedule> CalculateAsync(
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object>? sharedState = null)
    {
        MaterialSchedule deterministicSchedule = await RunDeterministicFallbackAsync(
            projectModel,
            drawings,
            dependencies,
            buildingType,
            cancellationToken,
            sharedState);

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve(drawings, dependencies);
        List<MaterialSchedule> groupSchedules = await CalculateGroupsWithLlmAsync(
            groups,
            projectModel,
            dependencies,
            buildingType,
            sharedState,
            cancellationToken);

        if (groupSchedules.Count == 0)
        {
            logger.LogInformation(
                "Material calculation used deterministic schedule only — grouped LLM returned no data.");
            return deterministicSchedule;
        }

        MaterialSchedule llmMerged = MaterialScheduleMerger.Merge(groupSchedules);
        MaterialSchedule merged = MaterialScheduleMerger.Overlay(deterministicSchedule, llmMerged);
        ApplyProjectModelRoofTimberFallback(merged, projectModel);
        merged = MaterialWasteApplier.Apply(merged);
        merged.Summary = MaterialScheduleBuilder.BuildSummaryPublic(merged);

        logger.LogInformation(
            "Material calculation merged {GroupCount} grouped LLM results onto deterministic schedule.",
            groupSchedules.Count);

        return MaterialQuantityFilter.PruneZeroQuantities(merged);
    }

    private async Task<List<MaterialSchedule>> CalculateGroupsWithLlmAsync(
        IReadOnlyList<MaterialDrawingGroup> groups,
        ProjectModel projectModel,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        IReadOnlyDictionary<string, object>? sharedState,
        CancellationToken cancellationToken)
    {
        List<MaterialSchedule> schedules = new();

        foreach (MaterialDrawingGroup group in groups)
        {
            if (group.Drawings.Count == 0)
            {
                continue;
            }

            try
            {
                MaterialSchedule? groupSchedule = await TryCalculateGroupWithLlmAsync(
                    group,
                    projectModel,
                    dependencies,
                    buildingType,
                    sharedState,
                    cancellationToken);

                if (groupSchedule is not null && MaterialScheduleContentHelper.HasMeaningfulContent(groupSchedule))
                {
                    schedules.Add(groupSchedule);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Grouped material calculation failed for {GroupLabel} ({DrawingCount} drawings).",
                    group.Label,
                    group.Drawings.Count);
            }
        }

        return schedules;
    }

    private async Task<MaterialSchedule?> TryCalculateGroupWithLlmAsync(
        MaterialDrawingGroup group,
        ProjectModel projectModel,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        IReadOnlyDictionary<string, object>? sharedState,
        CancellationToken cancellationToken)
    {
        MaterialCalculationGroupRequest request = new()
        {
            MaterialGroup = group.Kind.ToString(),
            MaterialGroupLabel = group.Label,
            BuildingType = buildingType,
            SharedState = sharedState ?? new Dictionary<string, object>(),
            ProjectModel = projectModel,
            TimberStructure = BuildTimberStructureHint(projectModel, group.Drawings),
            Drawings = group.Drawings.ToList(),
            Dependencies = FilterDependencies(dependencies, group.Drawings)
        };

        StringBuilder userPrompt = new();
        userPrompt.Append("materialGroup:");
        userPrompt.Append(JsonSerializer.Serialize(request, CompactJsonOptions));

        string response = await TechnicalDocumentationAgentInvoker.CompleteAsync(
            completionService,
            agentDefinitionLoader,
            AgentName,
            userPrompt.ToString(),
            cancellationToken);

        MaterialSchedule schedule = MaterialScheduleJsonParser.Parse(
            response,
            group.Drawings,
            buildingType,
            logger);

        if (!MaterialScheduleContentHelper.HasMeaningfulContent(schedule))
        {
            return null;
        }

        return schedule;
    }

    private static List<DrawingDependencyLink> FilterDependencies(
        IReadOnlyList<DrawingDependencyLink> dependencies,
        IReadOnlyList<FloorPlanDrawing> groupDrawings)
    {
        HashSet<string> keys = groupDrawings
            .Select(drawing => $"{drawing.Source.FileName}::{drawing.Source.PageNumber}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return dependencies
            .Where(link =>
            {
                string sourceKey = $"{link.SourceFileName}::{link.SourcePageNumber}";
                string? targetKey = link.TargetFileName is null || link.TargetPageNumber is null
                    ? null
                    : $"{link.TargetFileName}::{link.TargetPageNumber}";

                return keys.Contains(sourceKey)
                    || (targetKey is not null && keys.Contains(targetKey));
            })
            .ToList();
    }

    private static MaterialCalculationTimberStructureHint BuildTimberStructureHint(
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> groupDrawings)
    {
        List<TimberGroup> groups = TimberStructureCollector.CollectGroups(groupDrawings);
        if (groups.Count == 0 && projectModel.Roof.TimberGroups.Count > 0)
        {
            groups = projectModel.Roof.TimberGroups
                .Select(group => new TimberGroup
                {
                    Name = group.Element ?? string.Empty,
                    Section = group.Section ?? string.Empty,
                    GroupVolumeM3 = group.VolumeM3 ?? 0,
                    GroupSumMb = group.LengthM ?? 0
                })
                .ToList();
        }

        return new MaterialCalculationTimberStructureHint
        {
            TotalVolumeM3 = projectModel.Roof.TotalTimberVolumeM3
                ?? groups.Sum(group => group.GroupVolumeM3 ?? 0),
            Groups = groups
                .Select(group => new MaterialCalculationTimberGroupHint
                {
                    Name = group.Name,
                    Section = group.Section,
                    GroupVolumeM3 = group.GroupVolumeM3 ?? 0,
                    GroupSumMb = group.GroupSumMb ?? 0
                })
                .ToList()
        };
    }

    private async Task<MaterialSchedule> RunDeterministicFallbackAsync(
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        string buildingType,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object>? sharedState)
    {
        ProjectTechnicalDocumentationDetails legacyDetails = new()
        {
            ProjectModel = projectModel
        };
        ProjectTechnicalDocumentationDetailsBuilder.Apply(
            legacyDetails,
            projectModel,
            drawings,
            dependencies,
            computedSchedule: null,
            buildingType);

        return await materialOrchestrationFallback.OrchestrateAsync(
            drawings,
            dependencies,
            legacyDetails,
            buildingType,
            cancellationToken,
            sharedState);
    }

    private static void ApplyProjectModelRoofTimberFallback(MaterialSchedule schedule, ProjectModel projectModel)
    {
        double? totalVolumeM3 = projectModel.Roof.TotalTimberVolumeM3;
        if (totalVolumeM3 is not > 0)
        {
            return;
        }

        double mergedVolume = schedule.Roof.Timber.Sum(item => item.NetQuantity);
        if (mergedVolume >= totalVolumeM3.Value * 0.9)
        {
            return;
        }

        schedule.Roof.Timber.Clear();
        schedule.Roof.Timber.Add(new MaterialItem
        {
            Element = "Drewno więźby dachowej — łącznie",
            Calculation = $"Odczyt z tabeli K-04: totalVolumeM3 = {totalVolumeM3.Value:F3} m3",
            SourceType = "read",
            SourceDrawings = ["K-04"],
            NetQuantity = totalVolumeM3.Value,
            WastePercent = 10,
            GrossQuantity = Math.Round(totalVolumeM3.Value * 1.1, 2),
            Unit = "m3",
            Specification = projectModel.Roof.WoodClass is not null
                ? $"drewno {projectModel.Roof.WoodClass}"
                : "drewno konstrukcyjne"
        });
    }

    private sealed class MaterialCalculationGroupRequest
    {
        public string MaterialGroup { get; set; } = string.Empty;

        public string MaterialGroupLabel { get; set; } = string.Empty;

        public string BuildingType { get; set; } = string.Empty;

        public IReadOnlyDictionary<string, object> SharedState { get; set; } =
            new Dictionary<string, object>();

        public ProjectModel ProjectModel { get; set; } = new();

        public MaterialCalculationTimberStructureHint? TimberStructure { get; set; }

        public List<FloorPlanDrawing> Drawings { get; set; } = new();

        public List<DrawingDependencyLink> Dependencies { get; set; } = new();
    }

    private sealed class MaterialCalculationTimberStructureHint
    {
        public double? TotalVolumeM3 { get; set; }

        public List<MaterialCalculationTimberGroupHint> Groups { get; set; } = new();
    }

    private sealed class MaterialCalculationTimberGroupHint
    {
        public string? Name { get; set; }

        public string? Section { get; set; }

        public double GroupVolumeM3 { get; set; }

        public double GroupSumMb { get; set; }
    }
}
