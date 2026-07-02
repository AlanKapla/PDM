using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class RoomsPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IAggregationAgent aggregationAgent;

    public RoomsPipelineAgent(IAggregationAgent aggregationAgent)
    {
        this.aggregationAgent = aggregationAgent;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Rooms;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        ProjectModel projectModel = await aggregationAgent.AggregateAsync(context.Drawings, cancellationToken);

        context.ProjectModel = projectModel;
        context.Details.ProjectModel = projectModel;

        int roomCount = projectModel.Floors.Sum(floor => floor.Rooms.Count);
        string summary = $"Aggregated {roomCount} rooms across {projectModel.Floors.Count} floors.";

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: summary,
            Warnings: [],
            ContributedFields: ["ProjectModel", "Details.ProjectModel"]);
    }
}
