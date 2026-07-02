using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class AggregationAgentService : IAggregationAgent
{
    private readonly ILogger<AggregationAgentService> logger;

    public AggregationAgentService(ILogger<AggregationAgentService> logger)
    {
        this.logger = logger;
    }

    public Task<ProjectModel> AggregateAsync(
        IReadOnlyList<FloorPlanDrawing> drawings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (drawings.Count == 0)
        {
            return Task.FromResult(new ProjectModel());
        }

        logger.LogInformation("Using deterministic aggregation (no LLM)");
        ProjectModel model = ProjectModelFallbackBuilder.Build(drawings);
        return Task.FromResult(model);
    }
}
