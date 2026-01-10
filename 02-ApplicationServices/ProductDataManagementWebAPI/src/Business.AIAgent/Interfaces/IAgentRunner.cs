using Business.AIAgent.Models;

namespace Business.AIAgent.Interfaces;

/// <summary>
/// Executes the main agent loop: LLM -> Tool Calls -> LLM -> ...
/// Continues until LLM decides to stop or max iterations reached
/// </summary>
public interface IAgentRunner
{
    /// <summary>
    /// Runs the agent with given conversation and available tools
    /// </summary>
    /// <param name="messages">Initial conversation history (system prompt + user query)</param>
    /// <param name="tools">Available tools that the agent can use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final response from the agent and complete conversation history</returns>
    Task<AgentRunResult> RunAsync(
        List<LLMMessage> messages,
        List<ITool> tools,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an agent run
/// </summary>
public sealed class AgentRunResult
{
    /// <summary>
    /// Final response from the agent
    /// </summary>
    public LLMMessage FinalMessage { get; set; } = new();

    /// <summary>
    /// Complete conversation history including all tool calls and results
    /// </summary>
    public List<LLMMessage> ConversationHistory { get; set; } = new();

    /// <summary>
    /// Number of iterations (LLM calls) performed
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    /// Total tokens used across all LLM calls
    /// </summary>
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Tools that were executed during the run
    /// </summary>
    public List<ToolResult> ToolResults { get; set; } = new();

    /// <summary>
    /// Whether the agent completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the run failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Detailed trace of all LLM requests and responses for debugging
    /// </summary>
    public List<LLMResponse> LLMResponses { get; set; } = new();
}
