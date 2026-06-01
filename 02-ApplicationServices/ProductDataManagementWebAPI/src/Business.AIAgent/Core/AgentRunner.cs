using Azure.AI.OpenAI;
using Azure.Identity;
using Business.AIAgent.Abstractions;
using Business.AIAgent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Business.AIAgent.Core;

public sealed class AgentRunner : IAgentRunner
{
    private readonly AgentDefinitionLoader _loader;
    private readonly IToolRegistry _registry;
    private readonly ToolCallExecutor _executor;
    private readonly AzureAIAgentOptions _options;
    private readonly ILogger<AgentRunner> _logger;

    public AgentRunner(
        AgentDefinitionLoader loader,
        IToolRegistry registry,
        ToolCallExecutor executor,
        IOptions<AzureAIAgentOptions> options,
        ILogger<AgentRunner> logger)
    {
        _loader = loader;
        _registry = registry;
        _executor = executor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        string agentName,
        string userMessage,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        StringBuilder fullResponse = new();
        int iterations = 0;

        try
        {
            await foreach (AgentStreamEvent evt in RunStreamingAsync(agentName, userMessage, context, cancellationToken))
            {
                if (evt.Type == AgentStreamEventType.Token)
                {
                    fullResponse.Append(evt.Content);
                }
                if (evt.Type == AgentStreamEventType.Complete)
                {
                    iterations++;
                }
            }

            return AgentRunResult.Success(fullResponse.ToString(), iterations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentRunner failed for agent '{AgentName}'", agentName);
            return AgentRunResult.Failure(ex.Message);
        }
    }

    public async IAsyncEnumerable<AgentStreamEvent> RunStreamingAsync(
        string agentName,
        string userMessage,
        AgentContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (context.Depth > _options.MaxSubAgentDepth)
        {
            yield return AgentStreamEvent.ErrorEvent(
                $"Max sub-agent depth ({_options.MaxSubAgentDepth}) exceeded.",
                context.SessionId);
            yield break;
        }

        AgentDefinition definition = _loader.Load(agentName);
        ChatClient client = BuildChatClient(definition.Model);

        IReadOnlyList<IAgentTool> allowedTools = _registry.GetAllowedTools(definition.Tools);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(definition.SystemPrompt),
            new UserChatMessage(userMessage)
        ];

        ChatCompletionOptions completionOptions = BuildOptions(definition, allowedTools);

        int iteration = 0;
        while (iteration < definition.MaxIterations)
        {
            iteration++;
            _logger.LogDebug("Agent '{AgentName}' iteration {Iteration}", agentName, iteration);

            bool hasToolCalls = false;
            Dictionary<int, (string Id, string Name, StringBuilder Args)> accumulator = [];
            StringBuilder iterationContent = new();

            AsyncCollectionResult<StreamingChatCompletionUpdate> stream =
                client.CompleteChatStreamingAsync(messages, completionOptions, cancellationToken);

            await foreach (StreamingChatCompletionUpdate update in stream.ConfigureAwait(false))
            {
                foreach (ChatMessageContentPart part in update.ContentUpdate)
                {
                    if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                    {
                        iterationContent.Append(part.Text);
                        yield return AgentStreamEvent.TokenEvent(part.Text, context.SessionId);
                    }
                }

                foreach (StreamingChatToolCallUpdate toolCallUpdate in update.ToolCallUpdates)
                {
                    hasToolCalls = true;
                    int idx = toolCallUpdate.Index;

                    if (!accumulator.TryGetValue(idx, out (string Id, string Name, StringBuilder Args) entry))
                    {
                        entry = (string.Empty, string.Empty, new StringBuilder());
                    }

                    string newId = !string.IsNullOrEmpty(toolCallUpdate.ToolCallId)
                        ? toolCallUpdate.ToolCallId
                        : entry.Id;

                    string newName = !string.IsNullOrEmpty(toolCallUpdate.FunctionName)
                        ? toolCallUpdate.FunctionName
                        : entry.Name;

                    if (toolCallUpdate.FunctionArgumentsUpdate is not null)
                    {
                        entry.Args.Append(toolCallUpdate.FunctionArgumentsUpdate.ToString());
                    }

                    accumulator[idx] = (newId, newName, entry.Args);
                }
            }

            if (!hasToolCalls)
            {
                messages.Add(new AssistantChatMessage(iterationContent.ToString()));
                break;
            }

            List<ChatToolCall> pendingToolCalls = accumulator
                .OrderBy(kv => kv.Key)
                .Select(kv => ChatToolCall.CreateFunctionToolCall(
                    kv.Value.Id,
                    kv.Value.Name,
                    BinaryData.FromString(kv.Value.Args.ToString())))
                .ToList();

            messages.Add(new AssistantChatMessage(pendingToolCalls));

            foreach (ChatToolCall toolCall in pendingToolCalls)
            {
                yield return AgentStreamEvent.ToolCallStartEvent(toolCall.FunctionName, context.SessionId);

                (ChatMessage resultMessage, ToolResult result) = await _executor.ExecuteAsync(
                    toolCall, context, cancellationToken);
                messages.Add(resultMessage);

                yield return AgentStreamEvent.ToolCallResultEvent(
                    toolCall.FunctionName,
                    result.IsSuccess ? result.Content : $"ERROR: {result.ErrorMessage}",
                    context.SessionId);
            }
        }

        yield return AgentStreamEvent.CompleteEvent(context.SessionId);
    }

    private ChatClient BuildChatClient(string modelName)
    {
        string deployment = string.IsNullOrWhiteSpace(modelName)
            ? _options.DefaultDeployment
            : modelName;

        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(_options.Endpoint), new ApiKeyCredential(_options.ApiKey));

        return azureClient.GetChatClient(deployment);
    }

    private static ChatCompletionOptions BuildOptions(
        AgentDefinition definition,
        IReadOnlyList<IAgentTool> tools)
    {
        ChatCompletionOptions options = new()
        {
            Temperature = definition.Temperature,
            MaxOutputTokenCount = definition.MaxTokens
        };

        foreach (IAgentTool tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: BinaryData.FromObjectAsJson(tool.ParametersSchema)));
        }

        if (tools.Count > 0)
        {
            options.ToolChoice = ChatToolChoice.CreateAutoChoice();
        }

        return options;
    }
}
