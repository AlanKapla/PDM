using System.Text.Json.Serialization;

namespace Business.AIAgent.Models;

/// <summary>
/// Request sent to LLM
/// Generic and serializable for caching and debugging
/// </summary>
public sealed class LLMRequest
{
    /// <summary>
    /// Conversation history including system, user, assistant, and tool messages
    /// </summary>
    [JsonPropertyName("messages")]
    public List<LLMMessage> Messages { get; set; } = new();

    /// <summary>
    /// Available tools that the LLM can call
    /// </summary>
    [JsonPropertyName("tools")]
    public List<ToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature for response generation (0.0 - 2.0)
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p sampling (0.0 - 1.0)
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    /// <summary>
    /// Optional: User ID for tracking and rate limiting
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }

    /// <summary>
    /// Optional: Stream the response
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    /// <summary>
    /// Optional: Additional parameters specific to the provider
    /// </summary>
    [JsonPropertyName("additional_parameters")]
    public Dictionary<string, object>? AdditionalParameters { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Definition of a tool available to the LLM
/// </summary>
public sealed class ToolDefinition
{
    /// <summary>
    /// Type of tool (usually "function")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Function definition
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Definition of a function that can be called
/// </summary>
public sealed class FunctionDefinition
{
    /// <summary>
    /// Name of the function
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the function does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON Schema describing the function parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}
