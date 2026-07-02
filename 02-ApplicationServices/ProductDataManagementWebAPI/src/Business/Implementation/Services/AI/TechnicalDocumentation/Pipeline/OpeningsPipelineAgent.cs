using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class OpeningsPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    public string Name => TechnicalDocumentationPipelineAgentNames.Openings;

    public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        JoinerySummary joinery = TechnicalDocumentationDetailsAggregator.AggregateJoineryForLegacy(context.Drawings);
        context.Details.Joinery = joinery;

        int windowCount = joinery.Exterior?.Windows.Sum(item => item.Count) ?? 0;
        int doorCount = joinery.Exterior?.Doors.Sum(item => item.Count) ?? 0;
        string summary = $"Aggregated {windowCount} windows and {doorCount} doors.";

        return Task.FromResult(new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: summary,
            Warnings: [],
            ContributedFields: ["Details.Joinery"]));
    }
}
