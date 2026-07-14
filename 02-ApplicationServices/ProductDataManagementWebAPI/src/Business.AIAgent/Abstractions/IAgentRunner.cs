namespace Business.AIAgent.Abstractions;

public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(string agentName, string userMessage, AgentContext context, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AgentStreamEvent> RunStreamingAsync(string agentName, string userMessage, AgentContext context, CancellationToken cancellationToken = default);
}
