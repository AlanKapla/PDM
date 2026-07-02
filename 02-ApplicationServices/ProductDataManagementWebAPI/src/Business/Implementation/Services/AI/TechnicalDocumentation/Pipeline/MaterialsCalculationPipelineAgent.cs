using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class MaterialsCalculationPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IMaterialCalculationAgent materialCalculationAgent;
    private readonly TechnicalDocumentationOptions options;

    public MaterialsCalculationPipelineAgent(
        IMaterialCalculationAgent materialCalculationAgent,
        IOptions<TechnicalDocumentationOptions> options)
    {
        this.materialCalculationAgent = materialCalculationAgent;
        this.options = options.Value;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.MaterialsCalculation;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        ProjectModel projectModel = context.ProjectModel ?? new ProjectModel();
        string buildingType = TechnicalDocumentationPipelineHelpers.ResolveBuildingType(projectModel);

        MaterialSchedule materialSchedule;
        if (options.UseGroupPipeline && context.Drawings.Count == 0)
        {
            materialSchedule = await CalculateForGroupPipelineAsync(
                projectModel,
                buildingType,
                context,
                cancellationToken);
        }
        else
        {
            materialSchedule = await materialCalculationAgent.CalculateAsync(
                projectModel,
                context.Drawings.Count > 0 ? context.Drawings : [],
                context.Dependencies,
                buildingType,
                cancellationToken,
                context.SharedState);
        }

        context.ComputedMaterialSchedule = materialSchedule;

        int itemCount = materialSchedule.Summary.Count;
        string summary = $"Calculated material schedule with {itemCount} summary items.";

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: summary,
            Warnings: materialSchedule.Warnings.ToList(),
            ContributedFields: ["ComputedMaterialSchedule"]);
    }

    private async Task<MaterialSchedule> CalculateForGroupPipelineAsync(
        ProjectModel projectModel,
        string buildingType,
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        MaterialSchedule fromModel = ProjectModelMaterialScheduleBuilder.Build(projectModel, buildingType);

        if (MaterialScheduleContentHelper.HasMeaningfulContent(fromModel))
        {
            return fromModel;
        }

        MaterialSchedule legacySchedule = await materialCalculationAgent.CalculateAsync(
            projectModel,
            [],
            context.Dependencies,
            buildingType,
            cancellationToken,
            context.SharedState);

        if (MaterialScheduleContentHelper.HasMeaningfulContent(legacySchedule))
        {
            return legacySchedule;
        }

        return fromModel;
    }
}
