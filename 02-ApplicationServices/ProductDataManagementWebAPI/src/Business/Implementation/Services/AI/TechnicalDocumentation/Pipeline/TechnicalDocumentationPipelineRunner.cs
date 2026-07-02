using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class TechnicalDocumentationPipelineRunner : ITechnicalDocumentationPipelineRunner
{
    private readonly LegacyTechnicalDocumentationPipelineRunner legacyRunner;
    private readonly GroupTechnicalDocumentationPipelineRunner groupRunner;
    private readonly TechnicalDocumentationOptions options;
    private readonly ILogger<TechnicalDocumentationPipelineRunner> logger;

    public TechnicalDocumentationPipelineRunner(
        LegacyTechnicalDocumentationPipelineRunner legacyRunner,
        GroupTechnicalDocumentationPipelineRunner groupRunner,
        IOptions<TechnicalDocumentationOptions> options,
        ILogger<TechnicalDocumentationPipelineRunner> logger)
    {
        this.legacyRunner = legacyRunner;
        this.groupRunner = groupRunner;
        this.options = options.Value;
        this.logger = logger;
    }

    public Task<ProjectTechnicalDocumentationDetails> RunAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken)
    {
        if (options.UseGroupPipeline)
        {
            logger.LogInformation("Running group thematic pipeline (UseGroupPipeline=true)");
            return groupRunner.RunAsync(images, cancellationToken);
        }

        logger.LogInformation("Running legacy per-drawing pipeline (UseGroupPipeline=false)");
        return legacyRunner.RunAsync(images, cancellationToken);
    }
}
