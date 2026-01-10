using System.Diagnostics;
using Azure;
using Azure.Identity;
using Business.AIAgent.Configuration;
using Business.AIAgent.Interfaces;
using Business.AIAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Business.AIAgent.Services;

/// <summary>
/// Implementation of Azure OpenAI client using Azure SDK
/// </summary>
public sealed class AzureOpenAIClient : IAzureOpenAIClient, IAsyncDisposable
{
    private readonly AzureOpenAISettings settings;
    private readonly ILogger<AzureOpenAIClient> logger;
    private readonly Azure.AI.OpenAI.AzureOpenAIClient azureClient;
    private readonly ChatClient chatClient;

    public AzureOpenAIClient(
        IOptions<AzureOpenAISettings> options,
        ILogger<AzureOpenAIClient> logger)
    {
        settings = options.Value;
        this.logger = logger;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new ArgumentException("AzureOpenAI:Endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.DeploymentName))
        {
            throw new ArgumentException("AzureOpenAI:DeploymentName is not configured.");
        }

        if (settings.UseManagedIdentity)
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = false,
                ExcludeVisualStudioCodeCredential = false
            });
            
            azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(new Uri(settings.Endpoint), credential);
            logger.LogInformation("Azure OpenAI client initialized with Managed Identity");
        }
        else if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            var credential = new AzureKeyCredential(settings.ApiKey);
            azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(new Uri(settings.Endpoint), credential);
            logger.LogInformation("Azure OpenAI client initialized with API Key");
        }
        else
        {
            throw new ArgumentException("Either UseManagedIdentity must be true or ApiKey must be provided.");
        }

        chatClient = azureClient.GetChatClient(settings.DeploymentName);

        logger.LogInformation("AzureOpenAIClient initialized. Endpoint: {Endpoint}, Deployment: {Deployment}",
            settings.Endpoint, settings.DeploymentName);
    }

    public async Task<LLMResponse> GetCompletionAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Convert generic LLMMessage to Azure SDK ChatMessage
            var chatMessages = ConvertMessages(request.Messages);

            // Prepare chat options
            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxTokens ?? settings.MaxTokens,
                Temperature = (float)(request.Temperature ?? settings.Temperature),
            };

            if (request.TopP.HasValue)
            {
                chatOptions.TopP = (float)request.TopP.Value;
            }

            // Add tools if provided
            if (request.Tools != null && request.Tools.Count > 0)
            {
                foreach (var tool in request.Tools)
                {
                    var chatTool = ConvertTool(tool);
                    chatOptions.Tools.Add(chatTool);
                }
            }

            logger.LogDebug("Sending completion request to Azure OpenAI. Messages: {MessageCount}, Tools: {ToolCount}",
                chatMessages.Count, request.Tools?.Count ?? 0);

            // Call Azure OpenAI
            ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);

            stopwatch.Stop();

            // Convert response
            var response = ConvertResponse(completion, stopwatch.ElapsedMilliseconds);

            logger.LogInformation("Received completion from Azure OpenAI. Finish reason: {FinishReason}, Tokens: {Tokens}, Time: {Time}ms",
                response.FinishReason, response.Usage?.TotalTokens ?? 0, response.ResponseTimeMs);

            return response;
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Azure OpenAI API request failed. Status: {Status}, ErrorCode: {ErrorCode}",
                ex.Status, ex.ErrorCode);

            return new LLMResponse
            {
                Error = $"Azure OpenAI API error: {ex.ErrorCode} - {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Unexpected error calling Azure OpenAI");

            return new LLMResponse
            {
                Error = $"Unexpected error: {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async IAsyncEnumerable<LLMResponse> GetCompletionStreamAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Convert messages
        var chatMessages = ConvertMessages(request.Messages);

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = request.MaxTokens ?? settings.MaxTokens,
            Temperature = (float)(request.Temperature ?? settings.Temperature),
        };

        if (request.TopP.HasValue)
        {
            chatOptions.TopP = (float)request.TopP.Value;
        }

        // Add tools
        if (request.Tools != null && request.Tools.Count > 0)
        {
            foreach (var tool in request.Tools)
            {
                var chatTool = ConvertTool(tool);
                chatOptions.Tools.Add(chatTool);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        await foreach (var update in chatClient.CompleteChatStreamingAsync(chatMessages, chatOptions, cancellationToken))
        {
            // For now, yield a partial response per update
            // In production, you'd accumulate and yield complete chunks
            yield return new LLMResponse
            {
                Id = update.CompletionId ?? string.Empty,
                Message = new LLMMessage
                {
                    Role = MessageRole.Assistant,
                    Content = string.Join("", update.ContentUpdate.Select(c => c.Text))
                },
                FinishReason = update.FinishReason.HasValue ? ConvertFinishReason(update.FinishReason.Value) : FinishReason.Other,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private List<ChatMessage> ConvertMessages(List<LLMMessage> messages)
    {
        var chatMessages = new List<ChatMessage>();

        foreach (var msg in messages)
        {
            ChatMessage chatMessage = msg.Role switch
            {
                MessageRole.System => new SystemChatMessage(msg.Content ?? string.Empty),
                MessageRole.User => new UserChatMessage(msg.Content ?? string.Empty),
                MessageRole.Assistant when msg.ToolCalls != null && msg.ToolCalls.Count > 0 =>
                    CreateAssistantMessageWithTools(msg),
                MessageRole.Assistant => new AssistantChatMessage(msg.Content ?? string.Empty),
                MessageRole.Tool => new ToolChatMessage(msg.ToolCallId ?? string.Empty, msg.Content ?? string.Empty),
                _ => throw new ArgumentException($"Unsupported message role: {msg.Role}")
            };

            chatMessages.Add(chatMessage);
        }

        return chatMessages;
    }

    private AssistantChatMessage CreateAssistantMessageWithTools(LLMMessage msg)
    {
        var toolCalls = new List<ChatToolCall>();

        foreach (var tc in msg.ToolCalls!)
        {
            toolCalls.Add(ChatToolCall.CreateFunctionToolCall(
                tc.Id,
                tc.Function.Name,
                BinaryData.FromString(tc.Function.Arguments)));
        }

        // Create assistant message with tool calls
        var assistantMessage = new AssistantChatMessage(toolCalls);
        
        // If there's also content, we need to handle it differently
        // The SDK expects either content OR tool calls in most cases
        return assistantMessage;
    }

    private ChatTool ConvertTool(ToolDefinition tool)
    {
        // Convert parameters to BinaryData
        var parametersJson = System.Text.Json.JsonSerializer.Serialize(tool.Function.Parameters);
        var parametersData = BinaryData.FromString(parametersJson);

        return ChatTool.CreateFunctionTool(
            tool.Function.Name,
            tool.Function.Description,
            parametersData);
    }

    private LLMResponse ConvertResponse(ChatCompletion completion, long responseTimeMs)
    {
        var message = new LLMMessage
        {
            Role = MessageRole.Assistant,
            Content = string.Join("", completion.Content.Select(c => c.Text))
        };

        // Convert tool calls if present
        if (completion.ToolCalls.Count > 0)
        {
            message.ToolCalls = new List<Models.ToolCall>();

            foreach (var toolCall in completion.ToolCalls)
            {
                if (toolCall.Kind == ChatToolCallKind.Function)
                {
                    message.ToolCalls.Add(new Models.ToolCall
                    {
                        Id = toolCall.Id,
                        Type = "function",
                        Function = new FunctionCall
                        {
                            Name = toolCall.FunctionName,
                            Arguments = toolCall.FunctionArguments.ToString()
                        }
                    });
                }
            }
        }

        return new LLMResponse
        {
            Id = completion.Id,
            Message = message,
            FinishReason = ConvertFinishReason(completion.FinishReason),
            Usage = new TokenUsage
            {
                PromptTokens = completion.Usage.InputTokenCount,
                CompletionTokens = completion.Usage.OutputTokenCount,
                TotalTokens = completion.Usage.TotalTokenCount
            },
            Model = completion.Model,
            ResponseTimeMs = responseTimeMs
        };
    }

    private FinishReason ConvertFinishReason(ChatFinishReason reason)
    {
        return reason switch
        {
            ChatFinishReason.Stop => FinishReason.Stop,
            ChatFinishReason.Length => FinishReason.Length,
            ChatFinishReason.ToolCalls => FinishReason.ToolCalls,
            ChatFinishReason.ContentFilter => FinishReason.ContentFilter,
            _ => FinishReason.Other
        };
    }

    public async ValueTask DisposeAsync()
    {
        // Azure SDK clients don't require explicit disposal
        await Task.CompletedTask;
    }
}
