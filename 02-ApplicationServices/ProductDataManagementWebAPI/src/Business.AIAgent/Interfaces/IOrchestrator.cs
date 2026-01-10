using Business.AIAgent.Models;

namespace Business.AIAgent.Interfaces;

/// <summary>
/// High-level orchestrator that coordinates agent execution
/// Called from CQRS Command Handlers
/// Selects appropriate tools and prompts based on the task
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Executes an agent task with automatic tool selection
    /// </summary>
    /// <param name="systemPrompt">System prompt defining agent behavior and context</param>
    /// <param name="userQuery">User's query or task description</param>
    /// <param name="toolNames">Names of tools to make available (null = all registered tools)</param>
    /// <param name="additionalContext">Optional additional context (e.g., tenant ID, user info)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent execution result</returns>
    Task<AgentRunResult> ExecuteAsync(
        string systemPrompt,
        string userQuery,
        IEnumerable<string>? toolNames = null,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an agent task with explicitly provided tools
    /// </summary>
    /// <param name="systemPrompt">System prompt defining agent behavior and context</param>
    /// <param name="userQuery">User's query or task description</param>
    /// <param name="tools">Explicit list of tools to use</param>
    /// <param name="additionalContext">Optional additional context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent execution result</returns>
    Task<AgentRunResult> ExecuteWithToolsAsync(
        string systemPrompt,
        string userQuery,
        List<ITool> tools,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues an existing conversation
    /// </summary>
    /// <param name="conversationHistory">Existing conversation history</param>
    /// <param name="newUserMessage">New user message to append</param>
    /// <param name="toolNames">Names of tools to make available</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent execution result</returns>
    Task<AgentRunResult> ContinueConversationAsync(
        List<LLMMessage> conversationHistory,
        string newUserMessage,
        IEnumerable<string>? toolNames = null,
        CancellationToken cancellationToken = default);
}
