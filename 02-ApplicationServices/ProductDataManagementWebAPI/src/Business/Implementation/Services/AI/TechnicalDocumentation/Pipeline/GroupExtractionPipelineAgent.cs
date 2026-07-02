using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class GroupExtractionPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IGroupExtractionAgentService groupExtractionAgentService;
    private readonly ILogger<GroupExtractionPipelineAgent> logger;

    public GroupExtractionPipelineAgent(
        IGroupExtractionAgentService groupExtractionAgentService,
        ILogger<GroupExtractionPipelineAgent> logger)
    {
        this.groupExtractionAgentService = groupExtractionAgentService;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.GroupExtraction;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        if (context.ThematicGroups.Count == 0)
        {
            return new TechnicalDocumentationAgentResult(
                Success: false,
                AgentName: Name,
                Summary: "No thematic groups to extract.",
                Warnings: [],
                Error: new InvalidOperationException("No thematic groups to extract."));
        }

        List<GroupExtractionPairResult> results = [];
        List<string> warnings = [];

        foreach (ThematicDrawingGroup group in context.ThematicGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (group.Images.Count == 0)
            {
                warnings.Add($"Group {group.GroupName} has no images.");
                continue;
            }

            try
            {
                (string resultAJson, string resultBJson) = await groupExtractionAgentService.ExtractGroupAsync(
                    group,
                    cancellationToken);

                results.Add(new GroupExtractionPairResult
                {
                    GroupName = group.GroupName,
                    ResultAJson = resultAJson,
                    ResultBJson = resultBJson,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Group extraction failed for {GroupName}", group.GroupName);
                warnings.Add($"Group {group.GroupName} extraction failed: {ex.Message}");
            }
        }

        context.GroupExtractionResults.Clear();
        context.GroupExtractionResults.AddRange(results);

        bool success = results.Count > 0;
        return new TechnicalDocumentationAgentResult(
            Success: success,
            AgentName: Name,
            Summary: success
                ? $"Extracted {results.Count} of {context.ThematicGroups.Count} groups."
                : "All group extractions failed.",
            Warnings: warnings,
            Error: success ? null : new InvalidOperationException("All group extractions failed."),
            ContributedFields: ["GroupExtractionResults"]);
    }
}
