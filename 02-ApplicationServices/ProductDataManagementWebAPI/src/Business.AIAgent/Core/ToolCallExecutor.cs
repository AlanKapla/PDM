using Azure.AI.OpenAI;
using Business.AIAgent.Abstractions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace Business.AIAgent.Core;

/// <summary>
/// Executes tool calls returned by the LLM and produces tool result messages.
/// </summary>
public sealed class ToolCallExecutor
{
    private readonly IToolRegistry _registry;
    private readonly ILogger<ToolCallExecutor> _logger;

    public ToolCallExecutor(IToolRegistry registry, ILogger<ToolCallExecutor> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task<(ChatMessage resultMessage, ToolResult result)> ExecuteAsync(
        ChatToolCall toolCall,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        string toolName = toolCall.FunctionName;
        string rawArguments = toolCall.FunctionArguments.ToString();

        _logger.LogDebug("Executing tool '{ToolName}' with args: {Args}", toolName, rawArguments);

        IAgentTool? tool = _registry.Get(toolName);

        if (tool is null)
        {
            ToolResult notFound = ToolResult.Failure($"Unknown tool: '{toolName}'");
            return (BuildResultMessage(toolCall.Id, notFound), notFound);
        }

        JsonElement arguments;
        try
        {
            arguments = string.IsNullOrWhiteSpace(rawArguments)
                ? JsonDocument.Parse("{}").RootElement
                : JsonDocument.Parse(rawArguments).RootElement;
        }
        catch (JsonException ex)
        {
            ToolResult parseError = ToolResult.Failure($"Invalid JSON arguments: {ex.Message}");
            return (BuildResultMessage(toolCall.Id, parseError), parseError);
        }

        ToolResult toolResult = await tool.ExecuteAsync(arguments, context, cancellationToken);

        return (BuildResultMessage(toolCall.Id, toolResult), toolResult);
    }

    private static ChatMessage BuildResultMessage(string toolCallId, ToolResult result)
    {
        string content = result.IsSuccess
            ? result.Content
            : $"ERROR: {result.ErrorMessage}";

        return new ToolChatMessage(toolCallId, content);
    }
}
