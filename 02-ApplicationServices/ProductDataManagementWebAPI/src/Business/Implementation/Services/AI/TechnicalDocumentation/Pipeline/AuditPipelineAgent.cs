using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class AuditPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IAuditAgent auditAgent;

    public AuditPipelineAgent(IAuditAgent auditAgent)
    {
        this.auditAgent = auditAgent;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Audit;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        ProjectModel projectModel = context.ProjectModel ?? new ProjectModel();

        AuditResult auditResult = await auditAgent.AuditAsync(
            projectModel,
            context.ComputedMaterialSchedule,
            cancellationToken);

        if (context.ComputedMaterialSchedule is not null)
        {
            auditResult.Assumptions.AddRange(context.ComputedMaterialSchedule.Assumptions);
            auditResult.MissingMaterials.AddRange(context.ComputedMaterialSchedule.MissingDimensions);
            auditResult.Warnings.AddRange(context.ComputedMaterialSchedule.Warnings);
        }

        context.Details.AuditResult = auditResult;

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: "Audit completed for material schedule and project model.",
            Warnings: auditResult.Warnings,
            ContributedFields: ["Details.AuditResult"]);
    }
}
