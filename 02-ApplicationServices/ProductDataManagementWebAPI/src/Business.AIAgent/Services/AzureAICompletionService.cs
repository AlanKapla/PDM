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

    public Task<string> CompleteWithImageAsync(
        string systemPrompt,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        List<(byte[] ImageBytes, string MediaType)> images =
        [
            (imageBytes, mediaType)
        ];
        return CompleteWithImagesAsync(systemPrompt, images, cancellationToken);
    }

    public async Task<string> CompleteWithImagesAsync(
        string systemPrompt,
        IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
        CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0)
        {
            throw new ArgumentException("At least one image is required.", nameof(images));
        }

        ChatClient client = BuildChatClient();

        List<ChatMessageContentPart> contentParts = new List<ChatMessageContentPart>(images.Count);
        foreach ((byte[] ImageBytes, string MediaType) image in images)
        {
            contentParts.Add(
                ChatMessageContentPart.CreateImagePart(
                    BinaryData.FromBytes(image.ImageBytes),
                    image.MediaType));
        }

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(contentParts)
        ];
        ChatCompletionOptions options = new()
        {
            MaxOutputTokenCount = 4096,
            Temperature = 0,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
        ChatCompletion response = await client.CompleteChatAsync(
            messages,
            options,
            cancellationToken: cancellationToken);
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
