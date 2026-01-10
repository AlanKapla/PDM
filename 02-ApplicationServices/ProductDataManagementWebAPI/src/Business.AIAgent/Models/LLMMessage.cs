using System.Text.Json.Serialization;

namespace Business.AIAgent.Models;

/// <summary>
/// Role of the message sender in LLM conversation
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageRole
{
    /// <summary>
    /// System prompt defining agent behavior and context
    /// </summary>
    System,

    /// <summary>
    /// Message from the user
    /// </summary>
    User,

    /// <summary>
    /// Message from the AI assistant
    /// </summary>
    Assistant,

    /// <summary>
    /// Result returned from a tool execution
    /// </summary>
    Tool
}

/// <summary>
/// Generic message in LLM conversation
/// Serializable for storage and caching
/// </summary>
public sealed class LLMMessage
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    [JsonPropertyName("role")]
    public MessageRole Role { get; set; }

    /// <summary>
    /// Text content of the message
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Optional: Tool calls requested by the assistant
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Optional: ID of the tool call this message is responding to
    /// Used when role = Tool
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Optional: Name of the tool for tool messages
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional: Additional metadata for debugging and logging
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    public LLMMessage()
    {
    }

    public LLMMessage(MessageRole role, string content)
    {
        Role = role;
        Content = content;
    }

    /// <summary>
    /// Creates a system message
    /// </summary>
    public static LLMMessage System(string content) => new(MessageRole.System, content);

    /// <summary>
    /// Creates a user message
    /// </summary>
    public static LLMMessage User(string content) => new(MessageRole.User, content);

    /// <summary>
    /// Creates an assistant message
    /// </summary>
    public static LLMMessage Assistant(string content) => new(MessageRole.Assistant, content);

    /// <summary>
    /// Creates a tool result message
    /// </summary>
    public static LLMMessage Tool(string toolCallId, string toolName, string content) => new()
    {
        Role = MessageRole.Tool,
        Content = content,
        ToolCallId = toolCallId,
        Name = toolName
    };
}
