using System.Text.Json;
using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class VerificationPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IExtractionVerificationAgentService verificationAgentService;
    private readonly ILogger<VerificationPipelineAgent> logger;

    public VerificationPipelineAgent(
        IExtractionVerificationAgentService verificationAgentService,
        ILogger<VerificationPipelineAgent> logger)
    {
        this.verificationAgentService = verificationAgentService;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Verification;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        List<VerifiedGroupExtractionResult> verifiedResults = [];
        List<string> warnings = [];

        foreach (GroupExtractionPairResult pair in context.GroupExtractionResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExtractionDiffResult diff = ExtractionDiffEngine.Compare(pair.ResultAJson, pair.ResultBJson);
            string verifiedJson = pair.ResultAJson;
            bool agentCInvoked = false;

            ThematicDrawingGroup? group = context.ThematicGroups
                .FirstOrDefault(item => string.Equals(item.GroupName, pair.GroupName, StringComparison.Ordinal));

            if (group is null)
            {
                warnings.Add($"Group {pair.GroupName} not found in context.");
                continue;
            }

            if (diff.HasCriticalDifferences)
            {
                logger.LogWarning(
                    "Critical diff detected for group {GroupName}, invoking Agent C",
                    pair.GroupName);

                string agentCJson = await verificationAgentService.VerifyCriticalDiffsAsync(
                    group,
                    diff,
                    pair.ResultAJson,
                    pair.ResultBJson,
                    cancellationToken);

                verifiedJson = MergeVerifiedJson(pair.ResultAJson, agentCJson);
                agentCInvoked = true;
            }
            else if (diff.HasMinorDifferences)
            {
                foreach (ExtractionFieldDiff fieldDiff in diff.Differences.Where(item => !item.IsCritical))
                {
                    warnings.Add($"{pair.GroupName}.{fieldDiff.FieldPath}: minor diff A={fieldDiff.ValueA} B={fieldDiff.ValueB}");
                }
            }

            VerifiedGroupExtractionResult verified = new()
            {
                GroupName = pair.GroupName,
                VerifiedJson = verifiedJson,
                HadCriticalDiff = diff.HasCriticalDifferences,
                AgentCInvoked = agentCInvoked,
            };

            verified.Warnings.AddRange(warnings.Where(w => w.StartsWith(pair.GroupName, StringComparison.Ordinal)));
            verifiedResults.Add(verified);
        }

        context.VerifiedGroupExtractions.Clear();
        context.VerifiedGroupExtractions.AddRange(verifiedResults);
        context.PipelineWarnings.AddRange(warnings);

        bool success = verifiedResults.Count > 0;
        return new TechnicalDocumentationAgentResult(
            Success: success,
            AgentName: Name,
            Summary: $"Verified {verifiedResults.Count} group extraction results.",
            Warnings: warnings,
            Error: success ? null : new InvalidOperationException("Verification produced no results."),
            ContributedFields: ["VerifiedGroupExtractions"]);
    }

    private static string MergeVerifiedJson(string baseJson, string agentCJson)
    {
        if (string.IsNullOrWhiteSpace(agentCJson))
        {
            return baseJson;
        }

        return GroupExtractionJsonMerger.Merge([baseJson, agentCJson]);
    }
}
