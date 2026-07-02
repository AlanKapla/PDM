using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Interfaces.Configurations;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationAgentInvoker
{
    public static async Task<string> CompleteWithImageAsync(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        AgentDefinition definition = agentDefinitionLoader.Load(agentName);
        string systemPrompt = TechnicalDocumentationSystemPromptBuilder.ResolveSystemPrompt(agentDefinitionLoader, agentName);

        return await completionService.CompleteWithImageAsync(
            systemPrompt,
            imageBytes,
            mediaType,
            cancellationToken,
            definition.MaxTokens,
            definition.Temperature,
            jsonMode: true);
    }

    public static async Task<string> CompleteWithImageAndTextAsync(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        string userText,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken,
        string? systemPromptOverride = null,
        int? maxOutputTokensOverride = null)
    {
        AgentDefinition definition = agentDefinitionLoader.Load(agentName);
        string systemPrompt = systemPromptOverride
            ?? TechnicalDocumentationSystemPromptBuilder.ResolveSystemPrompt(agentDefinitionLoader, agentName);
        systemPrompt = TechnicalDocumentationSystemPromptBuilder.ApplySchemaReference(systemPrompt);
        int maxOutputTokens = maxOutputTokensOverride ?? definition.MaxTokens;

        return await completionService.CompleteWithImageAndTextAsync(
            systemPrompt,
            userText,
            imageBytes,
            mediaType,
            cancellationToken,
            maxOutputTokens,
            definition.Temperature,
            jsonMode: true);
    }

    public static async Task<string> CompleteWithImagesAsync(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        string? userText,
        IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
        CancellationToken cancellationToken,
        IOptions<TechnicalDocumentationOptions>? options = null,
        string? systemPromptOverride = null,
        int? maxOutputTokensOverride = null)
    {
        if (options is not null && images.Count > options.Value.MaxImagesPerGroup)
        {
            throw new InvalidOperationException(
                $"Image count {images.Count} exceeds MaxImagesPerGroup ({options.Value.MaxImagesPerGroup}).");
        }

        AgentDefinition definition = agentDefinitionLoader.Load(agentName);
        string systemPrompt = systemPromptOverride
            ?? TechnicalDocumentationSystemPromptBuilder.ResolveSystemPrompt(agentDefinitionLoader, agentName);
        systemPrompt = TechnicalDocumentationSystemPromptBuilder.ApplySchemaReference(systemPrompt);
        int maxOutputTokens = maxOutputTokensOverride ?? definition.MaxTokens;

        return await completionService.CompleteWithImagesAsync(
            systemPrompt,
            userText,
            images,
            cancellationToken,
            maxOutputTokens,
            definition.Temperature,
            jsonMode: true);
    }

    public static async Task<string> CompleteAsync(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        string agentName,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        AgentDefinition definition = agentDefinitionLoader.Load(agentName);
        string systemPrompt = TechnicalDocumentationSystemPromptBuilder.ResolveSystemPrompt(agentDefinitionLoader, agentName);

        return await completionService.CompleteAsync(
            systemPrompt,
            userPrompt,
            cancellationToken,
            definition.MaxTokens,
            definition.Temperature,
            jsonMode: true);
    }
}
