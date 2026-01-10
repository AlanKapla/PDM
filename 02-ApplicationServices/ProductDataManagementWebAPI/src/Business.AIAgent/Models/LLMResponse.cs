using System.Text.Json.Serialization;

namespace Business.AIAgent.Models;

/// <summary>
/// Reason why the LLM stopped generating
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FinishReason
{
    /// <summary>
    /// Natural completion of the response
    /// </summary>
    Stop,

    /// <summary>
    /// Reached maximum token limit
    /// </summary>
    Length,

    /// <summary>
    /// LLM requested to call one or more tools
    /// </summary>
    ToolCalls,

    /// <summary>
    /// Content filtered by safety system
    /// </summary>
    ContentFilter,

    /// <summary>
    /// Unknown or other reason
    /// </summary>
    Other
}

/// <summary>
/// Response received from LLM
/// Generic and serializable for caching and debugging
/// </summary>
public sealed class LLMResponse
{
    /// <summary>
    /// Unique identifier for this response
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The generated message from the assistant
    /// </summary>
    [JsonPropertyName("message")]
    public LLMMessage? Message { get; set; }

    /// <summary>
    /// Reason why the model stopped generating
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public FinishReason FinishReason { get; set; }

    /// <summary>
    /// Token usage statistics
    /// </summary>
    [JsonPropertyName("usage")]
    public TokenUsage? Usage { get; set; }

    /// <summary>
    /// Model used for generation
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Response timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Time taken to generate the response in milliseconds
    /// </summary>
    [JsonPropertyName("response_time_ms")]
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Optional: Error information if the request failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Optional: Additional metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Token usage statistics
/// </summary>
public sealed class TokenUsage
{
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of tokens in the completion
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used (prompt + completion)
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
