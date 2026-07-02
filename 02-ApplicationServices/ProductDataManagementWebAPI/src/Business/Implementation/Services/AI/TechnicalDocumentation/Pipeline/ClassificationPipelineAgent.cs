using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class ClassificationPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private readonly IDrawingClassificationAgent classificationAgent;
    private readonly ILogger<ClassificationPipelineAgent> logger;

    public ClassificationPipelineAgent(
        IDrawingClassificationAgent classificationAgent,
        ILogger<ClassificationPipelineAgent> logger)
    {
        this.classificationAgent = classificationAgent;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.Classification;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TechnicalDocumentationImageInput> images = context.PreparedImages.Count > 0
            ? context.PreparedImages
            : context.Images;

        Task<ClassifiedTechnicalDocumentationImage>[] classificationTasks = images
            .Select(image => ClassifySingleImageAsync(image, cancellationToken))
            .ToArray();

        ClassifiedTechnicalDocumentationImage[] classifiedImages = await Task.WhenAll(classificationTasks);
        context.ClassifiedImages.Clear();
        context.ClassifiedImages.AddRange(classifiedImages);
        context.Classifications.Clear();
        context.Classifications.AddRange(classifiedImages.Select(classifiedImage => classifiedImage.Classification));

        logger.LogInformation("Classification completed for {ImageCount} images", classifiedImages.Length);

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: $"Classified {classifiedImages.Length} images.",
            Warnings: [],
            ContributedFields: ["ClassifiedImages", "Classifications"]);
    }

    private async Task<ClassifiedTechnicalDocumentationImage> ClassifySingleImageAsync(
        TechnicalDocumentationImageInput image,
        CancellationToken cancellationToken)
    {
        DrawingClassification? obvious = ObviousDrawingTypeDetector.TryDetect(image.FileName);
        DrawingClassification classification;

        if (obvious is not null)
        {
            logger.LogInformation(
                "Skipping LLM classification for obvious drawing type {DrawingType} ({FileName})",
                obvious.DrawingType,
                image.FileName);
            classification = EnrichClassificationFromFileName(obvious, image.FileName);
        }
        else
        {
            classification = await classificationAgent.ClassifyAsync(
                image.ImageBytes,
                image.MediaType,
                cancellationToken);
            classification = EnrichClassificationFromFileName(classification, image.FileName);
        }

        return new ClassifiedTechnicalDocumentationImage
        {
            Image = image,
            Classification = classification,
        };
    }

    private static DrawingClassification EnrichClassificationFromFileName(
        DrawingClassification classification,
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(classification.SheetNumber))
        {
            classification.SheetNumber = DrawingSheetNumberInferrer.InferFromFileName(fileName);
        }

        return classification;
    }
}
