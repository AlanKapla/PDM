using Azure.AI.OpenAI;
using Azure.Identity;
using Business.AIAgent.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;

namespace Business.AIAgent.Services;

public sealed class AzureAICompletionService : IAICompletionService
{
    private readonly AzureAIAgentOptions options;
    private readonly ICompletionTokenUsageRecorder? tokenUsageRecorder;

    public AzureAICompletionService(
        IOptions<AzureAIAgentOptions> options,
        ICompletionTokenUsageRecorder? tokenUsageRecorder = null)
    {
        this.options = options.Value;
        this.tokenUsageRecorder = tokenUsageRecorder;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        int maxOutputTokens = 4096,
        float? temperature = null,
        bool jsonMode = false)
    {
        ChatClient client = BuildChatClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];
        ChatCompletionOptions completionOptions = BuildCompletionOptions(maxOutputTokens, temperature, jsonMode);

        ChatCompletion response = await TransientAiCompletionRetry.ExecuteAsync(
            ct => client.CompleteChatAsync(messages, completionOptions, cancellationToken: ct),
            cancellationToken);

        RecordTokenUsage(response);
        return response.Content[0].Text;
    }

    public async Task<string> CompleteWithImageAsync(
        string systemPrompt,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken,
        int maxOutputTokens = 4096,
        float? temperature = null,
        bool jsonMode = false)
    {
        ChatClient client = BuildChatClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType))
        ];
        ChatCompletionOptions completionOptions = BuildCompletionOptions(maxOutputTokens, temperature, jsonMode);
        ChatCompletion response = await TransientAiCompletionRetry.ExecuteAsync(
            ct => client.CompleteChatAsync(messages, completionOptions, cancellationToken: ct),
            cancellationToken);

        RecordTokenUsage(response);
        return response.Content[0].Text;
    }

    public async Task<string> CompleteWithImageAndTextAsync(
        string systemPrompt,
        string userText,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken,
        int maxOutputTokens = 4096,
        float? temperature = null,
        bool jsonMode = false)
    {
        ChatClient client = BuildChatClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(userText),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType))
        ];
        ChatCompletionOptions completionOptions = BuildCompletionOptions(maxOutputTokens, temperature, jsonMode);
        ChatCompletion response = await TransientAiCompletionRetry.ExecuteAsync(
            ct => client.CompleteChatAsync(messages, completionOptions, cancellationToken: ct),
            cancellationToken);

        RecordTokenUsage(response);
        return response.Content[0].Text;
    }

    public async Task<string> CompleteWithImagesAsync(
        string systemPrompt,
        string? userText,
        IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
        CancellationToken cancellationToken,
        int maxOutputTokens = 8192,
        float? temperature = null,
        bool jsonMode = false)
    {
        if (images.Count == 0)
        {
            throw new ArgumentException("At least one image is required.", nameof(images));
        }

        ChatClient client = BuildChatClient();
        List<ChatMessageContentPart> contentParts = [];

        if (!string.IsNullOrWhiteSpace(userText))
        {
            contentParts.Add(ChatMessageContentPart.CreateTextPart(userText));
        }

        foreach ((byte[] imageBytes, string mediaType) in images)
        {
            contentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType));
        }

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(contentParts)
        ];
        ChatCompletionOptions completionOptions = BuildCompletionOptions(maxOutputTokens, temperature, jsonMode);
        ChatCompletion response = await TransientAiCompletionRetry.ExecuteAsync(
            ct => client.CompleteChatAsync(messages, completionOptions, cancellationToken: ct),
            cancellationToken);

        RecordTokenUsage(response);
        return response.Content[0].Text;
    }

    private void RecordTokenUsage(ChatCompletion response)
    {
        if (tokenUsageRecorder is null)
        {
            return;
        }

        int totalTokens = ResolveTotalTokens(response);
        tokenUsageRecorder.Record(totalTokens);
    }

    private static int ResolveTotalTokens(ChatCompletion response)
    {
        if (response.Usage is null)
        {
            return 0;
        }

        if (response.Usage.TotalTokenCount > 0)
        {
            return response.Usage.TotalTokenCount;
        }

        return response.Usage.InputTokenCount + response.Usage.OutputTokenCount;
    }

    private static ChatCompletionOptions BuildCompletionOptions(
        int maxOutputTokens,
        float? temperature,
        bool jsonMode)
    {
        ChatCompletionOptions completionOptions = new()
        {
            MaxOutputTokenCount = maxOutputTokens
        };

        if (temperature.HasValue)
        {
            completionOptions.Temperature = temperature.Value;
        }

        if (jsonMode)
        {
            completionOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();
        }

        return completionOptions;
    }

    private ChatClient BuildChatClient()
    {
        AzureOpenAIClientOptions clientOptions = new()
        {
            NetworkTimeout = TimeSpan.FromMinutes(8)
        };

        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(options.ApiKey)
            ? new AzureOpenAIClient(new Uri(options.Endpoint), new DefaultAzureCredential(), clientOptions)
            : new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey), clientOptions);

        return azureClient.GetChatClient(options.DefaultDeployment);
    }
}
