namespace Business.AIAgent.Models;

/// <summary>
/// Base request model for agent interactions
/// </summary>
public sealed record AgentRequest
{
    /// <summary>
    /// Optional system prompt to set context and behavior
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// User prompt or question
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional context or additional parameters
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }

    /// <summary>
    /// Whether to enable streaming response
    /// </summary>
    public bool EnableStreaming { get; init; } = false;

    /// <summary>
    /// Whether to enable tool calling
    /// </summary>
    public bool EnableTools { get; init; } = false;

    /// <summary>
    /// Optional image content for Vision models (JPG, PNG)
    /// </summary>
    public byte[]? ImageContent { get; init; }

    /// <summary>
    /// MIME type of the image (e.g., "image/jpeg", "image/png")
    /// </summary>
    public string ImageMimeType { get; init; } = "image/jpeg";

    /// <summary>
    /// Tenant identifier for multi-tenancy
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Optional conversation history
    /// </summary>
    public List<AgentMessage>? History { get; init; }
}
