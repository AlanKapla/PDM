using System.Text.Json.Serialization;

namespace Business.AIAgent.Models;

/// <summary>
/// Result returned from a tool execution
/// </summary>
public sealed class ToolResult
{
    /// <summary>
    /// ID of the tool call this result corresponds to
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the tool that was executed
    /// </summary>
    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the tool execution succeeded
    /// </summary>
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Result content (can be JSON, text, etc.)
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional error message if execution failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    [JsonPropertyName("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Timestamp when the tool was executed
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional: Additional metadata about execution
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a successful tool result
    /// </summary>
    public static ToolResult Success(string toolCallId, string toolName, string content, long executionTimeMs)
    {
        return new ToolResult
        {
            ToolCallId = toolCallId,
            ToolName = toolName,
            IsSuccess = true,
            Content = content,
            ExecutionTimeMs = executionTimeMs
        };
    }

    /// <summary>
    /// Creates a failed tool result
    /// </summary>
    public static ToolResult Failure(string toolCallId, string toolName, string error, long executionTimeMs)
    {
        return new ToolResult
        {
            ToolCallId = toolCallId,
            ToolName = toolName,
            IsSuccess = false,
            Error = error,
            Content = string.Empty,
            ExecutionTimeMs = executionTimeMs
        };
    }
}
