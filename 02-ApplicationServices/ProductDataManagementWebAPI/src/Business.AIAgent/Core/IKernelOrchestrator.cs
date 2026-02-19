namespace Business.AIAgent.Core;

/// <summary>
/// Orchestrator interface for managing Semantic Kernel operations
/// </summary>
public interface IKernelOrchestrator
{
    /// <summary>
    /// Executes a prompt with optional system prompt and arguments
    /// </summary>
    Task<string> ExecutePromptAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with streaming response and optional system prompt
    /// </summary>
    IAsyncEnumerable<string> ExecutePromptStreamingAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with image content (for Vision models like GPT-4o)
    /// </summary>
    Task<string> ExecutePromptWithImageAsync(
        string prompt,
        byte[] imageContent,
        string mimeType = "image/jpeg",
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function with tools enabled and optional system prompt
    /// </summary>
    Task<string> ExecuteWithToolsAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a plugin by type
    /// </summary>
    void RegisterPlugin<TPlugin>() where TPlugin : class;

    /// <summary>
    /// Registers a plugin instance
    /// </summary>
    void RegisterPlugin(object plugin, string? pluginName = null);
}
