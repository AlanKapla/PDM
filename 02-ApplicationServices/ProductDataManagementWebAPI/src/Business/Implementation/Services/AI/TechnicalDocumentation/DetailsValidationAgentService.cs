using System.Text;
using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class DetailsValidationAgentService : IDetailsValidationAgent
{
    private const string ValidationAgentName = "details-validation-agent";
    private const string VisionAgentName = "details-validation-vision-agent";
    private const int MaxImageVerifications = 6;

    private static readonly JsonSerializerOptions CompactJsonOptions = TechnicalDocumentationJsonHelper.CreateCompactSerializerOptions();

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly TechnicalDocumentationOptions options;
    private readonly ILogger<DetailsValidationAgentService> logger;

    public DetailsValidationAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        IOptions<TechnicalDocumentationOptions> options,
        ILogger<DetailsValidationAgentService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<DetailsValidationResult> ValidateAsync(
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<TechnicalDocumentationPartialResult> partialResults,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DetailsValidationResult result = new();

        if (!options.EnableTestValidation || options.UseGroupPipeline)
        {
            return result;
        }

        try
        {
            JsonElement schemaReference = DetailsSchemaReferenceLoader.LoadSchemaReference();
            string expectedJson = schemaReference.GetRawText();
            string generatedJson = TechnicalDocumentationDetailsSerializer.Serialize(details);

            result.Differences = DetailsValidationDiffBuilder.Compare(expectedJson, generatedJson);

            logger.LogInformation(
                "Test validation diff: {DifferenceCount} differences between generated model and schema reference",
                result.Differences.Count);

            if (!options.EnableTestValidationAiReview)
            {
                return result;
            }

            DetailsValidationComparisonResponse comparison = await RunAiReviewAsync(
                schemaReference,
                generatedJson,
                result.Differences,
                drawings,
                cancellationToken);

            result.RootCauses = comparison.RootCauses;
            result.RemediationSteps = comparison.RemediationSteps;

            List<string> sheetsToVerify = comparison.SheetsToReverify
                .Where(sheet => !string.IsNullOrWhiteSpace(sheet))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxImageVerifications)
                .ToList();

            if (sheetsToVerify.Count == 0)
            {
                sheetsToVerify = ResolveDefaultSheetsToVerify(drawings);
            }

            List<DetailsValidationImageCheck> imageChecks = await RunImageVerificationsAsync(
                sheetsToVerify,
                result.Differences,
                generatedJson,
                images,
                drawings,
                partialResults,
                cancellationToken);

            result.ImageChecks = imageChecks;
            AppendImageFindingsToRemediation(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Details test validation failed — returning partial validation review");
            result.RootCauses.Add("Testowa walidacja modelu niedostępna — błąd podczas porównania.");
        }

        return result;
    }

    private async Task<DetailsValidationComparisonResponse> RunAiReviewAsync(
        JsonElement schemaReference,
        string generatedJson,
        IReadOnlyList<DetailsValidationDifference> knownDifferences,
        IReadOnlyList<FloorPlanDrawing> drawings,
        CancellationToken cancellationToken)
    {
        DetailsValidationComparisonRequest request = new()
        {
            DrawingCatalog = BuildDrawingCatalog(drawings)
        };

        StringBuilder userPrompt = new();
        userPrompt.Append("{\"schemaReference\":");
        userPrompt.Append(schemaReference.GetRawText());
        userPrompt.Append(",\"generatedDetails\":");
        userPrompt.Append(generatedJson);
        userPrompt.Append(",\"knownDifferences\":");
        userPrompt.Append(JsonSerializer.Serialize(knownDifferences, CompactJsonOptions));
        userPrompt.Append(",\"drawingCatalog\":");
        userPrompt.Append(JsonSerializer.Serialize(request.DrawingCatalog, CompactJsonOptions));
        userPrompt.Append('}');

        string response = await TechnicalDocumentationAgentInvoker.CompleteAsync(
            completionService,
            agentDefinitionLoader,
            ValidationAgentName,
            userPrompt.ToString(),
            cancellationToken);

        return TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            response,
            CompactJsonOptions,
            new DetailsValidationComparisonResponse(),
            logger,
            "DetailsValidationComparison");
    }

    private async Task<List<DetailsValidationImageCheck>> RunImageVerificationsAsync(
        IReadOnlyList<string> sheetsToVerify,
        IReadOnlyList<DetailsValidationDifference> differences,
        string generatedJson,
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<TechnicalDocumentationPartialResult> partialResults,
        CancellationToken cancellationToken)
    {
        List<DetailsValidationImageCheck> checks = new();

        foreach (string sheetNumber in sheetsToVerify)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TechnicalDocumentationImageInput? image = ResolveImageForSheet(sheetNumber, images, partialResults);
            if (image is null)
            {
                continue;
            }

            FloorPlanDrawing? drawing = partialResults
                .Select(result => result.Drawing)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Classification.SheetNumber, sheetNumber, StringComparison.OrdinalIgnoreCase))
                ?? drawings.FirstOrDefault(candidate =>
                    string.Equals(candidate.Classification.SheetNumber, sheetNumber, StringComparison.OrdinalIgnoreCase));

            if (drawing is null)
            {
                continue;
            }

            List<DetailsValidationDifference> sheetDifferences = differences
                .Where(difference => difference.SourceDrawings.Any(source =>
                    string.Equals(source, sheetNumber, StringComparison.OrdinalIgnoreCase))
                    || difference.Issue.Contains(sheetNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            DetailsValidationVisionRequest visionRequest = new()
            {
                SheetNumber = sheetNumber,
                DrawingType = drawing.Classification.DrawingType,
                Title = drawing.Classification.Title,
                DifferencesForSheet = sheetDifferences,
                GeneratedSnippet = ExtractJsonSnippet(generatedJson, sheetNumber)
            };

            StringBuilder userText = new();
            userText.Append(JsonSerializer.Serialize(visionRequest, CompactJsonOptions));

            try
            {
                string response = await TechnicalDocumentationAgentInvoker.CompleteWithImageAndTextAsync(
                    completionService,
                    agentDefinitionLoader,
                    VisionAgentName,
                    userText.ToString(),
                    image.ImageBytes,
                    image.MediaType,
                    cancellationToken);

                DetailsValidationImageCheck check = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
                    response,
                    CompactJsonOptions,
                    new DetailsValidationImageCheck(),
                    logger,
                    "DetailsValidationVision");

                if (!string.IsNullOrWhiteSpace(check.SheetNumber)
                    || check.Findings.Count > 0
                    || check.ConfirmedDifferences.Count > 0)
                {
                    check.SheetNumber = string.IsNullOrWhiteSpace(check.SheetNumber) ? sheetNumber : check.SheetNumber;
                    check.DrawingType = string.IsNullOrWhiteSpace(check.DrawingType)
                        ? drawing.Classification.DrawingType
                        : check.DrawingType;
                    checks.Add(check);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Vision validation failed for sheet {SheetNumber}", sheetNumber);
            }
        }

        return checks;
    }

    private static void AppendImageFindingsToRemediation(DetailsValidationResult result)
    {
        int order = result.RemediationSteps.Count > 0
            ? result.RemediationSteps.Max(step => step.Order) + 1
            : 1;

        foreach (DetailsValidationImageCheck check in result.ImageChecks)
        {
            foreach (string action in check.RecommendedActions)
            {
                if (string.IsNullOrWhiteSpace(action))
                {
                    continue;
                }

                result.RemediationSteps.Add(new DetailsValidationRemediationStep
                {
                    Order = order++,
                    Action = action,
                    Reason = $"Weryfikacja wizualna arkusza {check.SheetNumber}",
                    PipelineStage = "ImageExtraction",
                    SourceDrawings = [check.SheetNumber]
                });
            }

            result.RootCauses.AddRange(check.ConfirmedDifferences
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }
    }

    private static List<string> ResolveDefaultSheetsToVerify(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        string[] preferredTypes =
        [
            "rzut_parteru",
            "rzut_poddasza",
            "przekroj",
            "rzut_fundamentow",
            "rzut_wiezby_dachowej",
            "zagospodarowanie_terenu"
        ];

        List<string> sheets = new();

        foreach (string drawingType in preferredTypes)
        {
            string? sheet = drawings
                .Where(drawing => drawing.Classification.DrawingType.Contains(drawingType, StringComparison.OrdinalIgnoreCase))
                .Select(drawing => drawing.Classification.SheetNumber)
                .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

            if (!string.IsNullOrWhiteSpace(sheet))
            {
                sheets.Add(sheet);
            }
        }

        return sheets.Take(MaxImageVerifications).ToList();
    }

    private static TechnicalDocumentationImageInput? ResolveImageForSheet(
        string sheetNumber,
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<TechnicalDocumentationPartialResult> partialResults)
    {
        TechnicalDocumentationPartialResult? partial = partialResults.FirstOrDefault(result =>
            string.Equals(result.Drawing.Classification.SheetNumber, sheetNumber, StringComparison.OrdinalIgnoreCase));

        if (partial is null)
        {
            return null;
        }

        return images.FirstOrDefault(image =>
            string.Equals(image.FileName, partial.FileName, StringComparison.OrdinalIgnoreCase)
            && image.PageNumber == partial.PageNumber);
    }

    private static List<DetailsValidationCatalogEntry> BuildDrawingCatalog(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .Select(drawing => new DetailsValidationCatalogEntry
            {
                SheetNumber = drawing.Classification.SheetNumber,
                DrawingType = drawing.Classification.DrawingType,
                Title = drawing.Classification.Title,
                FileName = drawing.Source.FileName,
                PageNumber = drawing.Source.PageNumber
            })
            .ToList();
    }

    private static string ExtractJsonSnippet(string generatedJson, string sheetNumber)
    {
        if (generatedJson.Length <= 4000)
        {
            return generatedJson;
        }

        int index = generatedJson.IndexOf(sheetNumber, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return generatedJson[..4000];
        }

        int start = Math.Max(0, index - 1500);
        int length = Math.Min(3000, generatedJson.Length - start);
        return generatedJson.Substring(start, length);
    }

    private sealed class DetailsValidationComparisonRequest
    {
        public List<DetailsValidationCatalogEntry> DrawingCatalog { get; set; } = new();
    }

    private sealed class DetailsValidationComparisonResponse
    {
        public List<DetailsValidationDifference> Differences { get; set; } = new();
        public List<string> RootCauses { get; set; } = new();
        public List<DetailsValidationRemediationStep> RemediationSteps { get; set; } = new();
        public List<string> SheetsToReverify { get; set; } = new();
    }

    private sealed class DetailsValidationVisionRequest
    {
        public string SheetNumber { get; set; } = string.Empty;
        public string DrawingType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public List<DetailsValidationDifference> DifferencesForSheet { get; set; } = new();
        public string GeneratedSnippet { get; set; } = string.Empty;
    }

    private sealed class DetailsValidationCatalogEntry
    {
        public string? SheetNumber { get; set; }
        public string DrawingType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int PageNumber { get; set; }
    }
}
