using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationExtractionHelper
{
    internal const string FocusInstructionsPlaceholder = "{FOCUS_INSTRUCTIONS_PLACEHOLDER}";

    public static async Task<FloorPlanDrawing> ExtractWithFocusAsync(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        byte[] imageBytes,
        string mediaType,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext,
        string? focusPrompt,
        CancellationToken cancellationToken,
        ILogger? logger = null,
        bool useFocusB = false)
    {
        string userText = FloorPlanDrawingJsonParser.BuildExtractionUserText(
            classification,
            extractionContext,
            focusPrompt: null,
            includeFullTextSources: false);

        string systemPrompt = BuildSystemPrompt(
            agentDefinitionLoader,
            agentName,
            classification,
            focusPrompt,
            useFocusB,
            logger);

        int maxOutputTokens = ExtractionMaxTokensResolver.Resolve(classification.DrawingType);

        string response = await TechnicalDocumentationAgentInvoker.CompleteWithImageAndTextAsync(
            completionService,
            agentDefinitionLoader,
            agentName,
            userText,
            imageBytes,
            mediaType,
            cancellationToken,
            systemPrompt,
            maxOutputTokens);

        return FloorPlanDrawingJsonParser.Parse(response, classification);
    }

    internal static string BuildSystemPrompt(
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        DrawingClassification classification,
        string? focusPrompt,
        bool useFocusB,
        ILogger? logger = null)
    {
        string focusInstructions = ResolveFocusInstructions(classification, focusPrompt, useFocusB);
        string rawPrompt = agentDefinitionLoader.Load(agentName).SystemPrompt;
        string systemPrompt = rawPrompt.Replace(FocusInstructionsPlaceholder, focusInstructions, StringComparison.Ordinal);

        if (systemPrompt.Contains(FocusInstructionsPlaceholder, StringComparison.Ordinal))
        {
            logger?.LogWarning(
                "Focus placeholder still present for agent {AgentName} and drawing type {DrawingType}; applying fallback focus.",
                agentName,
                classification.DrawingType);

            (string fallbackA, string fallbackB) = ExtractionFocusPromptLoader.GetPrompts("default");
            string fallbackFocus = useFocusB ? fallbackB : fallbackA;
            systemPrompt = rawPrompt.Replace(FocusInstructionsPlaceholder, fallbackFocus, StringComparison.Ordinal);
        }

        return TechnicalDocumentationSystemPromptBuilder.ApplySchemaReference(systemPrompt);
    }

    private static string ResolveFocusInstructions(
        DrawingClassification classification,
        string? focusPrompt,
        bool useFocusB)
    {
        if (!string.IsNullOrWhiteSpace(focusPrompt))
        {
            return focusPrompt.Trim();
        }

        string normalizedType = ExtractionFocusRouter.NormalizeDrawingType(classification.DrawingType);
        (string focusA, string focusB) = ExtractionFocusPromptLoader.GetPrompts(normalizedType);
        return useFocusB ? focusB : focusA;
    }
}
