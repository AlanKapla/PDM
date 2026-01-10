using System.Text.Json.Serialization;

namespace Business.AIAgent.Models;

/// <summary>
/// Represents a tool call requested by the LLM
/// </summary>
public sealed class ToolCall
{
    /// <summary>
    /// Unique identifier for this tool call
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of the call (usually "function")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Function details
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

/// <summary>
/// Details of a function call within a tool call
/// </summary>
public sealed class FunctionCall
{
    /// <summary>
    /// Name of the function to call
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON-encoded arguments for the function
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
