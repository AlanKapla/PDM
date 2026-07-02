using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class GroupingPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly DrawingThematicGroupResolver groupResolver;
    private readonly ILogger<GroupingPipelineAgent> logger;

    public GroupingPipelineAgent(
        DrawingThematicGroupResolver groupResolver,
        ILogger<GroupingPipelineAgent> logger)
    {
        this.groupResolver = groupResolver;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Grouping;

    public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ThematicDrawingGroup> groups = groupResolver.Resolve(context.ClassifiedImages);
        context.ThematicGroups.Clear();
        context.ThematicGroups.AddRange(groups);

        logger.LogInformation(
            "Grouping resolved {GroupCount} thematic groups from {ImageCount} classified images",
            groups.Count,
            context.ClassifiedImages.Count);

        return Task.FromResult(new TechnicalDocumentationAgentResult(
            Success: groups.Count > 0,
            AgentName: Name,
            Summary: groups.Count > 0
                ? $"Resolved {groups.Count} thematic groups."
                : "No thematic groups resolved.",
            Warnings: groups.Count == 0 ? ["No thematic groups resolved."] : [],
            Error: groups.Count == 0
                ? new InvalidOperationException("No thematic groups resolved from classified images.")
                : null,
            ContributedFields: ["ThematicGroups"]));
    }
}
