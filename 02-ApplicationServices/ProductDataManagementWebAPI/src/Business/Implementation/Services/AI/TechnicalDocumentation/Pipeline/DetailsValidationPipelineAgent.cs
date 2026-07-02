using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class DetailsValidationPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IDetailsValidationAgent validationAgent;
    private readonly ILogger<DetailsValidationPipelineAgent> logger;

    public DetailsValidationPipelineAgent(
        IDetailsValidationAgent validationAgent,
        ILogger<DetailsValidationPipelineAgent> logger)
    {
        this.validationAgent = validationAgent;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.DetailsValidation;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        DetailsValidationResult validation = await validationAgent.ValidateAsync(
            context.Details,
            context.Images,
            context.Drawings,
            context.PartialResults,
            cancellationToken);

        context.Details.ValidationReview = validation;

        logger.LogInformation(
            "Details validation completed: {DifferenceCount} differences, {ImageCheckCount} image checks, {StepCount} remediation steps",
            validation.Differences.Count,
            validation.ImageChecks.Count,
            validation.RemediationSteps.Count);

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: $"Validated final model: {validation.Differences.Count} differences, {validation.RemediationSteps.Count} remediation steps.",
            Warnings: validation.Differences
                .Where(difference => string.Equals(difference.Severity, "high", StringComparison.OrdinalIgnoreCase))
                .Select(difference => $"{difference.Path}: {difference.Issue}")
                .ToList(),
            ContributedFields: ["Details.ValidationReview"]);
    }
}
