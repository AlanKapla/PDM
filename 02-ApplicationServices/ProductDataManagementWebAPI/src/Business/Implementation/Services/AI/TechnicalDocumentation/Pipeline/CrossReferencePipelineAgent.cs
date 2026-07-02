using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class CrossReferencePipelineAgent : ITechnicalDocumentationPipelineAgent
{
    public string Name => TechnicalDocumentationPipelineAgentNames.CrossReference;

    public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<DrawingDependencyLink> dependencies =
            TechnicalDocumentationCrossReferenceLinker.LinkDrawings(context.Drawings);

        context.Dependencies.Clear();
        context.Dependencies.AddRange(dependencies);

        TechnicalDocumentationSharedStatePropagator.Propagate(context.Drawings, context.SharedState);

        string summary = $"Linked {dependencies.Count} cross-references; propagated {context.SharedState.Count} shared values.";

        return Task.FromResult(new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: summary,
            Warnings: [],
            ContributedFields: ["Dependencies", "SharedState"]));
    }
}
