using System.Text;
using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class ConsolidationAgentService : IConsolidationAgentService
{
    private const string AgentName = "consolidation-agent";

    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly ILogger<ConsolidationAgentService> logger;

    public ConsolidationAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        ILogger<ConsolidationAgentService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.logger = logger;
    }

    public async Task<ProjectModel> ConsolidateAsync(
        IReadOnlyList<VerifiedGroupExtractionResult> verifiedGroups,
        CancellationToken cancellationToken)
    {
        if (verifiedGroups.Count == 0)
        {
            return new ProjectModel();
        }

        try
        {
            ProjectModel deterministic = ProjectModelFromVerifiedGroupsMapper.Map(verifiedGroups);

            Dictionary<string, string> groupPayload = verifiedGroups.ToDictionary(
                group => group.GroupName,
                group => group.VerifiedJson,
                StringComparer.Ordinal);

            string userPrompt = BuildUserPrompt(groupPayload);
            string response = await TechnicalDocumentationAgentInvoker.CompleteAsync(
                completionService,
                agentDefinitionLoader,
                AgentName,
                userPrompt,
                cancellationToken);

            ProjectModel? llmModel = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
                response,
                JsonOptions,
                new ProjectModel(),
                logger,
                "ProjectModel");

            ProjectModel model = ProjectModelFromVerifiedGroupsMapper.MergePreferNonEmpty(
                llmModel ?? new ProjectModel(),
                deterministic);

            model.ExtractionMetadata.ThematicGroups = verifiedGroups
                .Select(group => group.GroupName)
                .ToList();
            model.ExtractionMetadata.PipelineVersion = "group-v1";

            if (!ProjectModelFromVerifiedGroupsMapper.IsStructurallyPopulated(model))
            {
                logger.LogWarning(
                    "Consolidation produced structurally empty ProjectModel after deterministic mapping and LLM merge");
            }

            return model;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Consolidation LLM failed, using deterministic mapper from verified groups");
        }

        return ProjectModelFromVerifiedGroupsMapper.Map(verifiedGroups);
    }

    private static string BuildUserPrompt(Dictionary<string, string> groupPayload)
    {
        StringBuilder builder = new();
        builder.AppendLine("Skonsoliduj wyniki grup do ProjectModel według schematu §8.1.");
        builder.AppendLine(JsonSerializer.Serialize(groupPayload, JsonOptions));
        return builder.ToString();
    }
}
