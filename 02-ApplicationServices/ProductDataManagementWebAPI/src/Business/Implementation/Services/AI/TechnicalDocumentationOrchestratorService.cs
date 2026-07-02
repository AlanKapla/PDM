using Business.AIAgent.Services;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI;

public sealed class TechnicalDocumentationOrchestratorService : ITechnicalDocumentationOrchestratorService
{
    private readonly ITechnicalDocumentationPipelineRunner pipelineRunner;
    private readonly ICompletionTokenUsageRecorder tokenUsageRecorder;
    private readonly ILogger<TechnicalDocumentationOrchestratorService> logger;

    public TechnicalDocumentationOrchestratorService(
        ITechnicalDocumentationPipelineRunner pipelineRunner,
        ICompletionTokenUsageRecorder tokenUsageRecorder,
        ILogger<TechnicalDocumentationOrchestratorService> logger)
    {
        this.pipelineRunner = pipelineRunner;
        this.tokenUsageRecorder = tokenUsageRecorder;
        this.logger = logger;
    }

    public async Task<ProjectTechnicalDocumentationDetails> ProcessImagesAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken)
    {
        if (images.Count == 0)
        {
            throw new InvalidOperationException("No images provided for technical documentation processing.");
        }

        tokenUsageRecorder.Reset();

        ProjectTechnicalDocumentationDetails details = await pipelineRunner.RunAsync(images, cancellationToken);

        logger.LogInformation(
            "Technical documentation pipeline completed with {TokenUsage} total LLM tokens",
            details.TokenUsage);

        return details;
    }
}
