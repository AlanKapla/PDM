namespace Business.AIAgent.Models;

/// <summary>
/// Response model from agent interactions
/// </summary>
public sealed record AgentResponse
{
    /// <summary>
    /// Generated response content
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Indicates if the response was successful
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Tools that were invoked during execution
    /// </summary>
    public List<string>? InvokedTools { get; init; }

    /// <summary>
    /// Metadata about the response
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static AgentResponse Success(string content, List<string>? invokedTools = null)
        => new()
        {
            Content = content,
            IsSuccess = true,
            InvokedTools = invokedTools
        };

    public static AgentResponse Error(string errorMessage)
        => new()
        {
            Content = string.Empty,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}
