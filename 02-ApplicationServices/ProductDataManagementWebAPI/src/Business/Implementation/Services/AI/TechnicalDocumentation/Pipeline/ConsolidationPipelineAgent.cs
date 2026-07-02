using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class ConsolidationPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IConsolidationAgentService consolidationAgentService;
    private readonly ILogger<ConsolidationPipelineAgent> logger;

    public ConsolidationPipelineAgent(
        IConsolidationAgentService consolidationAgentService,
        ILogger<ConsolidationPipelineAgent> logger)
    {
        this.consolidationAgentService = consolidationAgentService;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Consolidation;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        ProjectModel consolidated = await consolidationAgentService.ConsolidateAsync(
            context.VerifiedGroupExtractions,
            cancellationToken);

        if (IsStructurallyEmpty(consolidated))
        {
            logger.LogWarning("Consolidation returned structurally empty model, merging verified group extractions");
            ProjectModel mapped = ProjectModelFromVerifiedGroupsMapper.Map(context.VerifiedGroupExtractions);
            consolidated = ProjectModelFromVerifiedGroupsMapper.MergePreferNonEmpty(consolidated, mapped);
        }

        if (IsStructurallyEmpty(consolidated))
        {
            logger.LogWarning("Consolidation still empty after group mapping, using classification metadata fallback");
            consolidated = BuildFallbackFromClassifications(context, consolidated);
        }

        context.ProjectModel = consolidated;

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: "Consolidated verified group extractions into ProjectModel.",
            Warnings: context.PipelineWarnings,
            ContributedFields: ["ProjectModel"]);
    }

    private static bool IsStructurallyEmpty(ProjectModel model)
    {
        return !ProjectModelFromVerifiedGroupsMapper.IsStructurallyPopulated(model);
    }

    private static ProjectModel BuildFallbackFromClassifications(
        TechnicalDocumentationAgentContext context,
        ProjectModel model)
    {
        ClassifiedTechnicalDocumentationImage? first = context.ClassifiedImages.FirstOrDefault();
        if (first is null)
        {
            return model;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Name))
        {
            model.Project.Name = first.Classification.ProjectName ?? first.Classification.Title;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Investor))
        {
            model.Project.Investor = first.Classification.Investor;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Location))
        {
            model.Project.Location = first.Classification.Location;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Author))
        {
            model.Project.Author = first.Classification.Author;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Date))
        {
            model.Project.Date = first.Classification.Date;
        }

        if (string.IsNullOrWhiteSpace(model.Project.Phase))
        {
            model.Project.Phase = first.Classification.Phase;
        }

        return model;
    }
}
