using Business.AIAgent.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class OutputPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly ICompletionTokenUsageRecorder tokenUsageRecorder;
    private readonly ILogger<OutputPipelineAgent> logger;

    public OutputPipelineAgent(
        ICompletionTokenUsageRecorder tokenUsageRecorder,
        ILogger<OutputPipelineAgent> logger)
    {
        this.tokenUsageRecorder = tokenUsageRecorder;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Output;

    public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProjectModel projectModel = context.ProjectModel ?? new ProjectModel();
        context.Details.ProjectModel = projectModel;
        context.Details.ProcessedAt = DateTimeOffset.UtcNow;
        context.Details.TokenUsage = tokenUsageRecorder.TotalTokens;

        TechnicalDocumentationGroupDetailsMapper.Apply(context.Details, projectModel, context.ComputedMaterialSchedule);

        if (context.PipelineWarnings.Count > 0)
        {
            foreach (string warning in context.PipelineWarnings)
            {
                projectModel.Warnings.Add(new ProjectModelWarning { Message = warning });
            }
        }

        if (context.FailedPages.Count > 0)
        {
            TechnicalDocumentationPipelineHelpers.AppendFailedPageWarnings(context.Details, context.FailedPages);
        }

        logger.LogInformation(
            "Group pipeline output completed with {TokenUsage} total LLM tokens",
            context.Details.TokenUsage);

        return Task.FromResult(new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: "Output assembled for group pipeline.",
            Warnings: context.PipelineWarnings,
            ContributedFields:
            [
                "Details.ProjectModel",
                "Details.MaterialSchedule",
                "Details.AuditResult",
                "Details.ProcessedAt",
                "Details.TokenUsage",
            ]));
    }
}
