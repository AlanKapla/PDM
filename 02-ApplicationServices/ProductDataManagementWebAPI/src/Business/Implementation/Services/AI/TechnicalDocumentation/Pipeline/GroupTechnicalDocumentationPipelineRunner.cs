using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class GroupTechnicalDocumentationPipelineRunner
{
    private static readonly string[][] AgentPhases =
    [
        [TechnicalDocumentationPipelineAgentNames.Ingestion],
        [TechnicalDocumentationPipelineAgentNames.Classification],
        [TechnicalDocumentationPipelineAgentNames.Grouping],
        [TechnicalDocumentationPipelineAgentNames.GroupExtraction],
        [TechnicalDocumentationPipelineAgentNames.Verification],
        [TechnicalDocumentationPipelineAgentNames.Consolidation],
        [TechnicalDocumentationPipelineAgentNames.MaterialsCalculation],
        [TechnicalDocumentationPipelineAgentNames.Audit],
        [TechnicalDocumentationPipelineAgentNames.Output],
        [TechnicalDocumentationPipelineAgentNames.DetailsValidation]
    ];

    private readonly IReadOnlyDictionary<string, ITechnicalDocumentationPipelineAgent> agentsByName;
    private readonly TechnicalDocumentationOptions options;
    private readonly ILogger<GroupTechnicalDocumentationPipelineRunner> logger;

    public GroupTechnicalDocumentationPipelineRunner(
        IEnumerable<ITechnicalDocumentationPipelineAgent> agents,
        IOptions<TechnicalDocumentationOptions> options,
        ILogger<GroupTechnicalDocumentationPipelineRunner> logger)
    {
        this.agentsByName = agents.ToDictionary(agent => agent.Name, StringComparer.Ordinal);
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<ProjectTechnicalDocumentationDetails> RunAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken)
    {
        TechnicalDocumentationAgentContext context = new()
        {
            Images = images.ToList(),
        };

        foreach (string[] phase in AgentPhases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RunPhaseAsync(phase, context, cancellationToken);
        }

        return context.Details;
    }

    private async Task RunPhaseAsync(
        IReadOnlyList<string> agentNames,
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        List<string> agentsToRun = agentNames
            .Where(ShouldRunAgent)
            .ToList();

        if (agentsToRun.Count == 0)
        {
            return;
        }

        foreach (string agentName in agentsToRun)
        {
            TechnicalDocumentationAgentResult result = await ExecuteAgentAsync(agentName, context, cancellationToken);
            TechnicalDocumentationPipelineHelpers.ApplyAgentResult(context, result);
        }
    }

    private async Task<TechnicalDocumentationAgentResult> ExecuteAgentAsync(
        string agentName,
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!agentsByName.TryGetValue(agentName, out ITechnicalDocumentationPipelineAgent? agent))
        {
            throw new InvalidOperationException($"Pipeline agent '{agentName}' is not registered.");
        }

        logger.LogInformation("Running group technical documentation pipeline agent: {AgentName}", agentName);

        TechnicalDocumentationAgentResult result = await agent.ExecuteAsync(context, cancellationToken);

        if (!result.Success)
        {
            throw result.Error ?? new InvalidOperationException($"Pipeline agent '{agentName}' failed.");
        }

        return result;
    }

    private bool ShouldRunAgent(string agentName)
    {
        if (options.EnableTestValidation)
        {
            return true;
        }

        return !string.Equals(
            agentName,
            TechnicalDocumentationPipelineAgentNames.DetailsValidation,
            StringComparison.Ordinal);
    }
}
