using Business.AIAgent.Models;

namespace Business.AIAgent.Services;

/// <summary>
/// High-level service for agent interactions
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Processes an agent request and returns a response
    /// System prompt can be provided via AgentRequest.SystemPrompt
    /// </summary>
    Task<AgentResponse> ProcessRequestAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a request with streaming response
    /// System prompt can be provided via AgentRequest.SystemPrompt
    /// </summary>
    IAsyncEnumerable<string> ProcessRequestStreamingAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default);
}
