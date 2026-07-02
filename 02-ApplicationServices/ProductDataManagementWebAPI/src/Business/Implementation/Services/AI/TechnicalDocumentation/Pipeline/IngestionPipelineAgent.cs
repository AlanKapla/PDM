using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class IngestionPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly ITechnicalDocumentationImagePreprocessor imagePreprocessor;
    private readonly ILogger<IngestionPipelineAgent> logger;

    public IngestionPipelineAgent(
        ITechnicalDocumentationImagePreprocessor imagePreprocessor,
        ILogger<IngestionPipelineAgent> logger)
    {
        this.imagePreprocessor = imagePreprocessor;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Ingestion;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        if (context.Images.Count == 0)
        {
            return new TechnicalDocumentationAgentResult(
                Success: false,
                AgentName: Name,
                Summary: "No images provided for ingestion.",
                Warnings: [],
                Error: new InvalidOperationException("No images provided for ingestion."));
        }

        Task<TechnicalDocumentationImageInput>[] preparationTasks = context.Images
            .Select(async image =>
            {
                (byte[] optimizedBytes, string optimizedMediaType) =
                    await imagePreprocessor.PrepareForVisionAsync(
                        image.ImageBytes,
                        image.MediaType,
                        cancellationToken);

                return image with
                {
                    ImageBytes = optimizedBytes,
                    MediaType = optimizedMediaType,
                };
            })
            .ToArray();

        TechnicalDocumentationImageInput[] preparedImages = await Task.WhenAll(preparationTasks);
        context.PreparedImages.Clear();
        context.PreparedImages.AddRange(preparedImages);

        logger.LogInformation("Ingestion prepared {ImageCount} images for vision", preparedImages.Length);

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: $"Prepared {preparedImages.Length} images.",
            Warnings: [],
            ContributedFields: ["PreparedImages"]);
    }
}
