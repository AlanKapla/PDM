using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

public sealed class ImageExtractionPipelineAgent : ITechnicalDocumentationPipelineAgent
{
    private static readonly SemaphoreSlim ImageSemaphore = new(3, 3);

    private readonly ITechnicalDocumentationImagePreprocessor imagePreprocessor;
    private readonly IDrawingClassificationAgent classificationAgent;
    private readonly IExtractionFocusRouter extractionFocusRouter;
    private readonly IArchitecturalExtractionAgent extractionAgentA;
    private readonly IExtractionAgentB extractionAgentB;
    private readonly IUniversalExtractionAgent universalExtractionAgent;
    private readonly IComparatorAgent comparatorAgent;
    private readonly ILogger<ImageExtractionPipelineAgent> logger;

    public ImageExtractionPipelineAgent(
        ITechnicalDocumentationImagePreprocessor imagePreprocessor,
        IDrawingClassificationAgent classificationAgent,
        IExtractionFocusRouter extractionFocusRouter,
        IArchitecturalExtractionAgent extractionAgentA,
        IExtractionAgentB extractionAgentB,
        IUniversalExtractionAgent universalExtractionAgent,
        IComparatorAgent comparatorAgent,
        ILogger<ImageExtractionPipelineAgent> logger)
    {
        this.imagePreprocessor = imagePreprocessor;
        this.classificationAgent = classificationAgent;
        this.extractionFocusRouter = extractionFocusRouter;
        this.extractionAgentA = extractionAgentA;
        this.extractionAgentB = extractionAgentB;
        this.universalExtractionAgent = universalExtractionAgent;
        this.comparatorAgent = comparatorAgent;
        this.logger = logger;
    }

    public string Name => TechnicalDocumentationPipelineAgentNames.ImageExtraction;

    public async Task<TechnicalDocumentationAgentResult> ExecuteAsync(
        TechnicalDocumentationAgentContext context,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TechnicalDocumentationImageInput> preparedImages =
            await PrepareImagesForVisionAsync(context.Images, cancellationToken);

        IReadOnlyList<DrawingClassification> classifications =
            await ClassifyAllImagesAsync(preparedImages, cancellationToken);

        IReadOnlyList<DrawingCatalogEntry> catalog =
            TechnicalDocumentationDrawingCatalog.Build(preparedImages, classifications);

        Task<TechnicalDocumentationPartialResult?>[] imageTasks = preparedImages
            .Select((image, index) => ProcessSingleImageAsync(
                image,
                classifications[index],
                catalog,
                cancellationToken))
            .ToArray();

        TechnicalDocumentationPartialResult?[] results = await Task.WhenAll(imageTasks);
        List<string> failedPages = TechnicalDocumentationPipelineHelpers.CollectFailedPages(preparedImages, results);

        List<TechnicalDocumentationPartialResult> validResults = results
            .Where(result => result is not null)
            .Select(result => result!)
            .ToList();

        context.PartialResults.AddRange(validResults);
        context.FailedPages.AddRange(failedPages);
        context.Drawings.AddRange(validResults.Select(result => result.Drawing));

        if (validResults.Count == 0)
        {
            return new TechnicalDocumentationAgentResult(
                Success: false,
                AgentName: Name,
                Summary: "All image extractions failed.",
                Warnings: failedPages,
                Error: new InvalidOperationException("All image extractions failed."));
        }

        string summary = $"Extracted {validResults.Count} of {preparedImages.Count} drawings.";

        return new TechnicalDocumentationAgentResult(
            Success: true,
            AgentName: Name,
            Summary: summary,
            Warnings: failedPages,
            ContributedFields: ["Drawings", "PartialResults", "FailedPages"]);
    }

    private async Task<IReadOnlyList<TechnicalDocumentationImageInput>> PrepareImagesForVisionAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken)
    {
        Task<TechnicalDocumentationImageInput>[] preparationTasks = images
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
                    MediaType = optimizedMediaType
                };
            })
            .ToArray();

        return await Task.WhenAll(preparationTasks);
    }

    private async Task<IReadOnlyList<DrawingClassification>> ClassifyAllImagesAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken)
    {
        Task<DrawingClassification>[] classificationTasks = images
            .Select(image => ClassifySingleImageAsync(image, cancellationToken))
            .ToArray();

        return await Task.WhenAll(classificationTasks);
    }

    private async Task<DrawingClassification> ClassifySingleImageAsync(
        TechnicalDocumentationImageInput image,
        CancellationToken cancellationToken)
    {
        DrawingClassification? obvious = ObviousDrawingTypeDetector.TryDetect(image.FileName);
        if (obvious is not null)
        {
            logger.LogInformation(
                "Skipping LLM classification for obvious drawing type {DrawingType} ({FileName})",
                obvious.DrawingType,
                image.FileName);
            return EnrichClassificationFromFileName(obvious, image.FileName);
        }

        DrawingClassification classified = await classificationAgent.ClassifyAsync(
            image.ImageBytes,
            image.MediaType,
            cancellationToken);

        return EnrichClassificationFromFileName(classified, image.FileName);
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

    private async Task<TechnicalDocumentationPartialResult?> ProcessSingleImageAsync(
        TechnicalDocumentationImageInput image,
        DrawingClassification classification,
        IReadOnlyList<DrawingCatalogEntry> catalog,
        CancellationToken cancellationToken)
    {
        await ImageSemaphore.WaitAsync(cancellationToken);

        try
        {
            ExtractionFocusRoute route = extractionFocusRouter.Resolve(classification);
            TechnicalDocumentationExtractionContext extractionContext =
                TechnicalDocumentationDrawingCatalog.BuildExtractionContext(image, classification, catalog);

            FloorPlanDrawing validated = route.RequiresCrossValidation
                ? await RunCrossValidationExtractionAsync(
                    image,
                    classification,
                    extractionContext,
                    route,
                    cancellationToken)
                : await RunUniversalExtractionAsync(
                    image,
                    classification,
                    extractionContext,
                    route,
                    cancellationToken);

            validated.Source = new DrawingSource
            {
                FileName = image.FileName,
                PageNumber = image.PageNumber
            };
            validated.Classification = classification;

            return new TechnicalDocumentationPartialResult(
                image.FileName,
                image.PageNumber,
                validated,
                route.RequiresCrossValidation);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to process image {FileName} page {PageNumber}",
                image.FileName, image.PageNumber);
            return null;
        }
        finally
        {
            ImageSemaphore.Release();
        }
    }

    private async Task<FloorPlanDrawing> RunCrossValidationExtractionAsync(
        TechnicalDocumentationImageInput image,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext extractionContext,
        ExtractionFocusRoute route,
        CancellationToken cancellationToken)
    {
        Task<FloorPlanDrawing> extractATask = extractionAgentA.ExtractAsync(
            image.ImageBytes,
            image.MediaType,
            classification,
            extractionContext,
            route.FocusA,
            cancellationToken);

        Task<FloorPlanDrawing> extractBTask = extractionAgentB.ExtractAsync(
            image.ImageBytes,
            image.MediaType,
            classification,
            extractionContext,
            route.FocusB,
            cancellationToken);

        await Task.WhenAll(extractATask, extractBTask);

        FloorPlanDrawing resultA = await extractATask;
        FloorPlanDrawing resultB = await extractBTask;

        return await comparatorAgent.CompareAsync(
            image.ImageBytes,
            image.MediaType,
            resultA,
            resultB,
            classification,
            cancellationToken);
    }

    private async Task<FloorPlanDrawing> RunUniversalExtractionAsync(
        TechnicalDocumentationImageInput image,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext extractionContext,
        ExtractionFocusRoute route,
        CancellationToken cancellationToken)
    {
        return await universalExtractionAgent.ExtractAsync(
            image.ImageBytes,
            image.MediaType,
            classification,
            extractionContext,
            route.FocusA,
            cancellationToken);
    }
}
