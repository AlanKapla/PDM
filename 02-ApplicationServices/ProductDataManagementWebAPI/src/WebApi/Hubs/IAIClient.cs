using Business.AIAgent;

namespace WebApi.Hubs;

public interface IAIClient
{
    Task OnToken(string token, string sessionId);
    Task OnToolCallStart(string toolName, string sessionId);
    Task OnToolCallResult(string toolName, string result, string sessionId);
    Task OnSubAgentStart(string agentName, string sessionId);
    Task OnSubAgentComplete(string agentName, string sessionId);
    Task OnComplete(string sessionId);
    Task OnError(string error, string sessionId);
}
