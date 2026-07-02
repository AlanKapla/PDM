using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class DrawingClassificationAgentService : IDrawingClassificationAgent
{
    private const string AgentName = "drawing-classification-agent";

    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    private static readonly string[] MaterialTableKeywords =
    [
        "lista prętów", "lista pretow", "lista drewna", "zestawienie pomieszczeń",
        "zestawienie pomieszczen", "zbrojenie", "harmonogram", "zestawienie"
    ];

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly ILogger<DrawingClassificationAgentService> logger;

    public DrawingClassificationAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        ILogger<DrawingClassificationAgentService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.logger = logger;
    }

    public async Task<DrawingClassification> ClassifyAsync(
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        string response = await TechnicalDocumentationAgentInvoker.CompleteWithImageAsync(
            completionService,
            agentDefinitionLoader,
            AgentName,
            imageBytes,
            mediaType,
            cancellationToken);

        DrawingClassification classification = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            response,
            JsonOptions,
            new DrawingClassification { DrawingType = "nieznany" },
            logger,
            "DrawingClassification");

        NormalizeClassification(classification);
        return classification;
    }

    private static void NormalizeClassification(DrawingClassification classification)
    {
        if (string.IsNullOrWhiteSpace(classification.DrawingType))
        {
            classification.DrawingType = "nieznany";
        }

        SyncLegacyTableFields(classification);
        DetectMaterialTable(classification);
        InferFloorMetadata(classification);
    }

    private static void SyncLegacyTableFields(DrawingClassification classification)
    {
        if (string.IsNullOrWhiteSpace(classification.TableContent)
            && !string.IsNullOrWhiteSpace(classification.DrawingTable))
        {
            classification.TableContent = classification.DrawingTable;
        }

        if (string.IsNullOrWhiteSpace(classification.DrawingTable)
            && !string.IsNullOrWhiteSpace(classification.TableContent))
        {
            classification.DrawingTable = classification.TableContent;
        }
    }

    private static void DetectMaterialTable(DrawingClassification classification)
    {
        if (classification.HasMaterialTable)
        {
            return;
        }

        string combined = BuildCombinedText(classification);
        if (string.IsNullOrWhiteSpace(combined))
        {
            return;
        }

        string lower = combined.ToLowerInvariant();
        foreach (string keyword in MaterialTableKeywords)
        {
            if (lower.Contains(keyword, StringComparison.Ordinal))
            {
                classification.HasMaterialTable = true;
                return;
            }
        }
    }

    private static void InferFloorMetadata(DrawingClassification classification)
    {
        if (!string.IsNullOrWhiteSpace(classification.FloorLevel)
            && classification.FloorOrder.HasValue)
        {
            return;
        }

        string normalized = ExtractionFocusRouter.NormalizeDrawingType(classification.DrawingType);
        string? title = classification.Title?.ToLowerInvariant();

        if (normalized.Contains("parter", StringComparison.Ordinal))
        {
            classification.FloorLevel ??= "Parter";
            classification.FloorOrder ??= 0;
            return;
        }

        if (normalized.Contains("poddasze", StringComparison.Ordinal))
        {
            classification.FloorLevel ??= "Poddasze";
            classification.FloorOrder ??= 99;
            return;
        }

        if (normalized.Contains("piwnic", StringComparison.Ordinal))
        {
            classification.FloorLevel ??= "Piwnica";
            classification.FloorOrder ??= -1;
            return;
        }

        if (normalized.Contains("piętro", StringComparison.Ordinal)
            || normalized.Contains("pietro", StringComparison.Ordinal))
        {
            if (title is not null
                && (title.Contains("ii", StringComparison.Ordinal)
                    || title.Contains("2", StringComparison.Ordinal)))
            {
                classification.FloorLevel ??= "Piętro 2";
                classification.FloorOrder ??= 2;
            }
            else
            {
                classification.FloorLevel ??= "Piętro 1";
                classification.FloorOrder ??= 1;
            }
        }
    }

    private static string BuildCombinedText(DrawingClassification classification)
    {
        return string.Join(" ",
            classification.TableTitle,
            classification.TableContent,
            classification.DrawingTable,
            classification.DescriptiveText,
            classification.TechnicalParameters);
    }
}
