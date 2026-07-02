using Business.AIAgent.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class ReportPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IAuditAgent auditAgent;
    private readonly ICompletionTokenUsageRecorder tokenUsageRecorder;
    private readonly ILogger<ReportPipelineAgent> logger;

    public ReportPipelineAgent(
        IAuditAgent auditAgent,
        ICompletionTokenUsageRecorder tokenUsageRecorder,
        ILogger<ReportPipelineAgent> logger)
    {
        this.auditAgent = auditAgent;
        this.tokenUsageRecorder = tokenUsageRecorder;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Report;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        ProjectModel projectModel = context.ProjectModel ?? new ProjectModel();
        string buildingType = TechnicalDocumentationPipelineHelpers.ResolveBuildingType(projectModel);

        ProjectTechnicalDocumentationDetailsBuilder.Apply(
            context.Details,
            projectModel,
            context.Drawings,
            context.Dependencies,
            context.ComputedMaterialSchedule,
            buildingType,
            context.Images,
            context.FailedPages);

        TechnicalDocumentationPipelineHelpers.AppendFailedPageWarnings(context.Details, context.FailedPages);
        context.Details.ValidationSummaries =
            TechnicalDocumentationPipelineHelpers.BuildValidationSummaries(context.PartialResults);

        context.Details.AuditResult = await auditAgent.AuditAsync(
            projectModel,
            context.ComputedMaterialSchedule,
            cancellationToken);

        if (context.ComputedMaterialSchedule is not null)
        {
            context.Details.AuditResult.Assumptions.AddRange(context.ComputedMaterialSchedule.Assumptions);
            context.Details.AuditResult.MissingMaterials.AddRange(context.ComputedMaterialSchedule.MissingDimensions);
            context.Details.AuditResult.Warnings.AddRange(context.ComputedMaterialSchedule.Warnings);
        }

        context.Details.ProcessedAt = DateTimeOffset.UtcNow;
        context.Details.TokenUsage = tokenUsageRecorder.TotalTokens;

        ProjectModelSection81Enricher.Enrich(context.Details);
        ProjectModelSection81Enricher.EnrichFromDrawings(projectModel, context.Drawings);

        logger.LogInformation(
            "Technical documentation report completed with {TokenUsage} total LLM tokens",
            context.Details.TokenUsage);

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: "Final report assembled with audit and structured project details.",
            Warnings: context.FailedPages,
            ContributedFields:
            [
                "Details.Project",
                "Details.Rooms",
                "Details.Floors",
                "Details.MaterialSchedule",
                "Details.AuditResult",
                "Details.ValidationSummaries"
            ]);
    }
}
