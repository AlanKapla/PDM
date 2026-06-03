using Azure.AI.OpenAI;
using Azure.Identity;
using Business.AIAgent.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;

namespace Business.AIAgent.Services;

public sealed class AzureAICompletionService : IAICompletionService
{
    private readonly AzureAIAgentOptions _options;

    public AzureAICompletionService(IOptions<AzureAIAgentOptions> options)
    {
        _options = options.Value;
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
        ChatCompletionOptions options = new() { MaxOutputTokenCount = null };
        if (temperature.HasValue)
            options.Temperature = temperature.Value;
        if (jsonMode)
            options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        ChatCompletion response = await client.CompleteChatAsync(messages, options, cancellationToken: cancellationToken);
        return response.Content[0].Text;
    }

    public async Task<string> CompleteWithImageAsync(
        string systemPrompt,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        ChatClient client = BuildChatClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType))
        ];
        ChatCompletion response = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Content[0].Text;
    }

    private ChatClient BuildChatClient()
    {
        AzureOpenAIClientOptions clientOptions = new()
        {
            NetworkTimeout = TimeSpan.FromMinutes(8)
        };

        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential(), clientOptions)
            : new AzureOpenAIClient(new Uri(_options.Endpoint), new ApiKeyCredential(_options.ApiKey), clientOptions);

        return azureClient.GetChatClient(_options.DefaultDeployment);
    }
}
