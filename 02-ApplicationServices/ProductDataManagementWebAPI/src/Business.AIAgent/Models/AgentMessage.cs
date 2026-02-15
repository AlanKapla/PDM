namespace Business.AIAgent.Models;

/// <summary>
/// Represents a message in agent conversation history
/// </summary>
public sealed record AgentMessage
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public required AgentRole Role { get; init; }

    /// <summary>
    /// Message content
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Timestamp of the message
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Role of the message sender
/// </summary>
public enum AgentRole
{
    User,
    Assistant,
    System,
    Tool
}
