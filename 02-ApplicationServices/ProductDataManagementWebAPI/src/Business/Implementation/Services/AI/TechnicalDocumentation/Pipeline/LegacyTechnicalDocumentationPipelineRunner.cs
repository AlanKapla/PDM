using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class LegacyTechnicalDocumentationPipelineRunner
{
    private static readonly string[][] AgentPhases =
    [
        [TechnicalDocumentationPipelineAgentNames.ImageExtraction],
        [
            TechnicalDocumentationPipelineAgentNames.CrossReference,
            TechnicalDocumentationPipelineAgentNames.Rooms,
            TechnicalDocumentationPipelineAgentNames.Openings
        ],
        [TechnicalDocumentationPipelineAgentNames.MaterialsCalculation],
        [TechnicalDocumentationPipelineAgentNames.Report],
        [TechnicalDocumentationPipelineAgentNames.DetailsValidation]
    ];

    private readonly IReadOnlyDictionary<string, ITechnicalDocumentationPipelineAgent> agentsByName;
    private readonly TechnicalDocumentationOptions options;
    private readonly ILogger<LegacyTechnicalDocumentationPipelineRunner> logger;

    public LegacyTechnicalDocumentationPipelineRunner(
        IEnumerable<ITechnicalDocumentationPipelineAgent> agents,
        IOptions<TechnicalDocumentationOptions> options,
        ILogger<LegacyTechnicalDocumentationPipelineRunner> logger)
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

        if (agentsToRun.Count == 1)
        {
            TechnicalDocumentationAgentResult result = await ExecuteAgentAsync(agentsToRun[0], context, cancellationToken);
            TechnicalDocumentationPipelineHelpers.ApplyAgentResult(context, result);
            return;
        }

        logger.LogInformation(
            "Running legacy technical documentation pipeline phase in parallel: {AgentNames}",
            string.Join(", ", agentsToRun));

        Task<TechnicalDocumentationAgentResult>[] tasks = agentsToRun
            .Select(agentName => ExecuteAgentAsync(agentName, context, cancellationToken))
            .ToArray();

        TechnicalDocumentationAgentResult[] results = await Task.WhenAll(tasks);

        foreach (TechnicalDocumentationAgentResult result in results)
        {
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

        logger.LogInformation("Running legacy technical documentation pipeline agent: {AgentName}", agentName);

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
